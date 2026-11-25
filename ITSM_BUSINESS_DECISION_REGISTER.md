# ITSM Business Decision Register

**Status:** Prepared for owner review. Not approved. Not an instruction to implement.
**Prepared:** 2026-11-24
**Source specification:** `ITSM_TARGET_OPERATING_WORKFLOW_SPECIFICATION.md` section 18 (proposed 2026-11-23)
**HEAD inspected:** `f1f4bdb`
**Mode:** Read-only source inspection. No application code, database object, SQL script, or existing document was modified. No SQL was executed. No UAT or LIVE connection was opened. Nothing was staged or committed.

This register extracts the 18 decisions in specification section 18. Each one stays a separate question. A recommendation is evidence and advice. It does not close the decision, record a signature, or authorise a build.

Evidence labels:

| Label | Meaning |
|---|---|
| Source-verified | Read in the current ITSM source, a migration, or the live sign-in path during this review |
| Not executed | The code path is visible; deployed rows, role grants, and runtime results were not queried |
| Specification assumption | What the target specification says it will assume until an owner rules. That assumption is not an approval |

Identifier warning: `ITSM_AS_IS_Findings_and_Risks.md` section 10 uses D-1 through D-10 for a different and shorter list. Those identifiers are not the identifiers in this register. Approvers should sign the question text in this file. A crosswalk is in section 8 so the two lists are not treated as the same decisions.

---

## 1. Executive summary

ITSM today is one ticket, one transition map, and two approval flags on request type. Incident, service request, and change are prefixes and names. They do not have separate lifecycles. Company is stored on the ticket as a snapshot. Queues that agents use to find work do not filter by that snapshot. The person who raised the ticket can resolve it, reassign it, and override its priority. Those behaviours are implemented on purpose in the current source. Changing them is a business ruling.

None of the 18 decisions is approved. Sixteen are business policy. Two, D-4 and D-8, are technical methods that still need an owner because they change a number employees quote and a link employees click. Several business decisions carry a direct security, tenant, or audit consequence: D-1, D-2, D-3, D-10, and D-15, together with the session contract in section 3.

The specification's working assumptions are reasonable starting points for a first operating model. They are not safe to treat as silent defaults. Two assumptions should be decided as a package with their dependencies before anyone writes workflow code:

- D-2 (take process rights away from the requester) cannot ship until D-14 names who may take an unassigned ticket. With the requester removed and no assignee, the current authorization function leaves nobody who can move the ticket.
- D-1 (leave All Open cross-company) matches the code's shared-service design. Approving it accepts that holders of `ITSM.Queue.View` see other companies' titles, requester names, and assignee names, and can search description text. Rejecting it is a tenant redesign, not a defect fix, and it has to cover dispatch, reassignment, and tickets whose company was never recorded.

Implementation of the target workflow stays unauthorised until the blocking set in section 5 is marked Approved or explicitly Deferred by the owner role named in this register. A Deferred row needs a written ruling. An empty row is not a deferral.

---

## 2. How to use this register

1. Read section 3 before ruling on D-1, D-3, or D-15. ITSM does not keep company or branch identity in the session keys the pages actually check.
2. Rule each decision by writing the chosen option in section 6. Leave the recommended option unmarked if you choose another one.
3. Do not combine decisions. D-9 does not approve D-10. D-7 does not approve D-5.
4. Recommendations that differ from the specification's working assumption are marked in the decision. D-16 is the clear case.
5. After the blocking set is ruled, a separate instruction is still required before application or database work starts.

---

## 3. Session contract and company isolation

This section is a constraint on every decision. It is not a nineteenth decision, and it is not an approval to change login.

### 3.1 Keys the ITSM pages use

Source-verified on the sign-in button path `login.aspx.cs` `ProcessSignIn`, and on the four ITSM pages:

| Key | Set by the live sign-in path | What ITSM does with it |
|---|---|---|
| `WORKMAN` | Employee code from `MST_UserMaster.EmployeeCode` | Actor for create, queues, detail, and every mutation. `BasePage.CurrentEmployeeCode` reads this key |
| `USERID` | Numeric user id, same value as `CurrentUserId` | Presence check only. Create, list, and detail redirect to login when it is null. Permission lookup prefers `CurrentUserId` and falls back to `USERID` only when that value parses as an integer |
| `BRANCH` | `MST_UserMaster.BranchId` as a string. That column is the legacy `Branch_ID`, not `MST_Branch.Id` | Presence check only. Create, list, and detail do not read the value. Ticket branch is resolved again from the user master inside `TicketService.CreateTicket` |
| `CurrentUserId` | Numeric user id | Used by `BasePage.HasPermission` to load `UserRole` permissions. ITSM page gates do not read it directly |

`ApprovalConfig.aspx.cs` checks `USERID` and `BRANCH` only, then requires `ITSM.ApprovalConfig.Manage`. The actor it records is still `WORKMAN`.

No C# assignment to a session key containing `Company` exists under `AnmolDristi`. There is no `Session["CompanyID"]` and no `Session["BranchID"]` in the ITSM pages. ADR 0001 names the existing session model (`LoginToken`, `WORKMAN`, `BasePage`, `RequirePermission`). It does not introduce a company session key.

Older methods in `login.aspx.cs` (`CredentialCheckerSecure_Debug_OLD2`, `CredentialChecker1`) set `USERID` to the employee code. They are not the sign-in button path. ITSM must keep working if a session was created by a path that still fills `USERID` with a non-numeric value, because the page gate only tests that the key is present and the actor is `WORKMAN`. Permission checks on that older shape depend on `CurrentUserId` still being numeric. This review did not change login.

### 3.2 Where company is actually decided

| Moment | Rule | Source |
|---|---|---|
| Create | Refuse the ticket when the requester's live `MST_UserMaster.CompanyId` is null or not positive. Copy that value onto `Tickets.CompanyId` | `TicketService.CreateTicket` |
| Mutate | Refuse all six update methods when `Tickets.CompanyId` is null | `TicketUpdateService.RequireRecordedTenant` |
| Audit | `TicketAuditLog.CompanyId` is the ticket snapshot and is non-nullable | `TicketUpdateService.AddAudit`, `TicketAuditLog` |
| View by relationship | Requester, assignee, or pending approver may open the ticket with no company comparison | `ItsmTicketAccess.CanViewTicket` |
| View by permission | `ITSM.Ticket.View` allows the ticket only when `Tickets.CompanyId` equals the actor's **current** `MST_UserMaster.CompanyId` | `ItsmTicketAccess.CanViewTicket`, `ItsmTenantContext.ResolveCompanyId` |
| Queues | No company predicate | `TicketListService.BuildQueueQuery` |

`ItsmTenantContext` states that the live user-master company must not gate ticket detail or ticket update. The permission view path is the exception: it compares the ticket snapshot with the actor's current company. An employee whose HR company changes can still open and process a ticket as requester or assignee. The same employee loses the permission view path for tickets snapshotted to the old company.

### 3.3 Migration implications if new session keys or a session tenant filter were adopted

These implications are why this register does not adopt `Session["BranchID"]`, `Session["CompanyID"]`, or a queue filter based on either key.

- Login does not set `BranchID` or `CompanyID`. Gating ITSM on those keys would send every current session back to login.
- `Session["BRANCH"]` is the legacy branch business key captured at sign-in. `Tickets.BranchId` is `MST_Branch.Id`, resolved at create. Using the session value as the ticket branch would write the wrong key and would ignore a later HR branch change that `TicketService` would otherwise read.
- A company value stored in session would go stale for the life of the session after an HR transfer. The permission path already re-reads `MST_UserMaster.CompanyId` on each detail load. A session copy would be a second, weaker source.
- Filtering All Open by the actor's current company would hide tickets the shared desk was given by branch-and-category dispatch, and would hide tickets whose `CompanyId` is null. That is decision D-1 plus D-15, not a session change.
- Replacing `WORKMAN` as the actor key would break create, queues, approval, and audit `ActorCode`, which all store the employee code.

Any future tenant filter belongs on `Tickets.CompanyId`, with an owner ruling under D-1 and D-15. It does not belong on a new session key.

---

## 4. Classification and dependencies

### 4.1 Business policy and technical method

| ID | Kind | Why this kind |
|---|---|---|
| D-1 | Business, with security effect | Who may see another company's tickets |
| D-2 | Business, with security and workflow effect | Who may resolve, reassign, reprioritise, close, and reopen |
| D-3 | Business, with a permission-enforcement consequence | Who may raise a ticket |
| D-4 | Technical method | How uniqueness is enforced. Business confirms the public number format stays stable |
| D-5 | Business | Whether one pause status is enough |
| D-6 | Business | Reopen window and what reopen does to a clock |
| D-7 | Business scope, with technical prerequisites | Whether a stored SLA and a breach mail are in this build |
| D-8 | Technical method | Which public site address mail may use. The owner must supply the value |
| D-9 | Business | Which request types require approval. Applied later as data, not as hard-coded C# |
| D-10 | Business, with cross-company authority effect | Who may authorise a change |
| D-11 | Business, with workflow-integrity effect | Whether a rejected ticket can be corrected in place |
| D-12 | Business | Whether age may close a ticket |
| D-13 | Business | Whether a score may block closure |
| D-14 | Business | Whether ownership is one mapped employee or a group |
| D-15 | Business and data policy, with security effect | Fate of tickets that have no company snapshot |
| D-16 | Business | Whether history must freeze the requester's department |
| D-17 | Business | Whether "a resolution" means any non-blank text |
| D-18 | Business | Whether a duplicate may be recorded as text |

### 4.2 Dependency map

An arrow means the first decision should be ruled before the second is implemented. It does not mean the first answer forces the second answer.

| Decision | Depends on a ruling of | What breaks if the dependency is ignored |
|---|---|---|
| D-2 | D-14 | Removing the requester's process right freezes every unassigned ticket, because `CanProcessTicket` is true only for the requester or the assignee |
| D-1 | D-15 | A company filter drops null-company rows out of the queue with no remediation path |
| D-1 | D-14 | Company-scoped queues disagree with branch-and-category dispatch, which has no company column |
| D-6 | D-2 | The reopen actor is the same gate as resolve and reassign |
| D-6 | D-7 | "Does reopen restart the SLA?" has no stored clock to restart until D-7 is in scope |
| D-7 | D-5 | Pause-on-hold has no business meaning until On Hold has a reason |
| D-7 | D-6 | Breach maths change if reopen resets the due time |
| D-7 | D-8 | A breach mail with the current link is not clickable |
| D-9 | D-10 | Standard and Normal cannot both be true on one request-type row. The representation depends on whether change class stays implied by flags |
| D-11 | D-4 | A new ticket that quotes the old number is ambiguous while numbers can collide |
| D-18 | D-4 | "Duplicate of {number}" is the same ambiguous reference |
| D-18 | D-2 | Only a process actor can write the resolution text that carries the duplicate sentence |
| D-3 | Session contract | Turning on a permission uses `CurrentUserId`. It must not be implemented by inventing a company or branch session key |
| D-8 | None for the value | The value is independent. Notification work waits on it |
| D-12, D-13, D-16 | None | Each can be ruled "out of this build" without blocking the others |

D-4 and D-8 can be ruled in parallel with the business set. D-4 should be ruled before any feature that treats the ticket number as a permanent external reference.

### 4.3 Special-attention index

| Topic requested | Where it is decided |
|---|---|
| Requester permission to resolve and reassign | D-2. Closure and reopen use the same gate and are called out there |
| Cross-company visibility and All Open | D-1 |
| CompanyId isolation | Section 3, D-1, D-15 |
| Session contract `WORKMAN`, `USERID`, `BRANCH` | Section 3 |
| Rejected-ticket correction and resubmission | D-11 |
| Ticket number uniqueness | D-4 |
| SLA persistence | D-7, with the reopen effect in D-6 |
| Change approval authority | D-10 and D-9 |
| On Hold semantics | D-5 |
| Notification requirements | Section 4.4. No decision in the 18 is "which events send mail." D-8 is the link. D-7 includes the breach mail |
| Resolution and closure authorization | D-2 for the actor. D-17 for the text. D-12 and D-13 for automatic close and CSAT |

### 4.4 Notifications, which are not their own decision

Source-verified: the only ITSM insert into `dbo.NotificationQueue` is `TicketService.EnqueueTicketCreatedNotification`, inside the create transaction.

- The recipient is the assignee email when an assignee was resolved and has an email. Otherwise the requester email.
- An empty email skips the insert and the ticket still commits.
- A SQL failure rolls the ticket back.
- The link written into the body is the literal `~/ITSM/UI/TicketDetail.aspx?id=` plus the internal id. The application does not expand `~` inside that string. No ITSM class reads a site address from configuration.
- Approve, reject, reassign, hold, resolve, close, and breach do not enqueue mail.
- The subject and title are encoded. The link is not a user-typed value.

The specification section 14 proposes additional mails. That matrix has no D-id. Approving D-8 supplies the link prefix. It does not, by itself, approve the recipient list. When leadership approves the operating model, they should mark section 14 of the specification as accepted, reduced, or deferred in the ruling column of D-8. This register does not add a nineteenth decision.

---

## 5. Decisions that block implementation

The specification section 17 already says not to start the build sequence until section 18 is answered. This review splits that gate into rows that block the first workflow build and rows that block only their own feature.

### 5.1 Block the first workflow build

These stay blocking while Status is Pending. An explicit Deferred ruling with a written option removes the block for that row.

| ID | Why the build waits |
|---|---|
| D-1 | The first person to "fix" queues would add a company filter the specification forbids until this is ruled |
| D-2 | Every process mutation uses the requester-or-assignee gate this decision changes |
| D-5 | Hold reason, waiting queues, and SLA pause all assume the status model |
| D-11 | Rejection recovery changes whether Rejected is a dead end |
| D-14 | Required before D-2 can be implemented without freezing unassigned tickets |
| D-4 | The specification places number integrity before workflow rules that quote the number. Mail, audit text, and duplicate references depend on it |

### 5.2 Block only the feature they name

| ID | Feature that waits |
|---|---|
| D-3 | Enforcing `UI.ITSM_CreateTicket.View` or `IsActiveEmployee` on create |
| D-6 | A reopen window or a change to SLA maths on reopen |
| D-7 | Persisted SLA, pause, and breach mail |
| D-8 | Absolute links in any mail, including the existing creation mail |
| D-9 | Hard-coding or reseeding IR/SR/CR approval flags. Administrators can already edit flags; that screen is not this decision |
| D-10 | CAB, a change-class column, or create-time guards for "requester is the IT Head" |
| D-12 | Any closure job |
| D-13 | A CSAT form or a closure block |
| D-15 | Any backfill or unfreeze of null `CompanyId` |
| D-16 | A department column or a department display |
| D-17 | A minimum length beyond non-blank |
| D-18 | A parent-ticket column. Text in the summary waits on D-4 and D-2 |

### 5.3 Technical validation that is not one of the 18

These do not have an owner question in section 18. They still block a demonstration of closure and of SLA numbers. They are checks, not policy.

| Check | Why it matters | State |
|---|---|---|
| Deployed `MST_TicketStatus.IsTerminal` for Resolved | `ChangeStatus` refuses a terminal current status before it reads the map. The repository seed sets Resolved `IsTerminal = false`, and the seed runs only when the table is empty. A deployed terminal Resolved makes both close and reopen dead. The as-is findings list calls a related question D-1. That is a different identifier | Not executed |
| Deployed request-type prefixes and approval flags | D-9 cannot be compared with production until a named environment is read | Not executed |
| `VMS_PlantTimings.Branch_ID` versus the id passed into `SlaService` | Create passes legacy `MST_UserMaster.BranchId` into SLA and `MST_Branch.Id` into dispatch | Not executed |
| Role grants actually present for `ITSM.Queue.View` and `UI.ITSM_CreateTicket.View` | Scripts exist. Execution was not checked | Not executed |

---

## 6. The 18 decisions

### D-1

| Field | Content |
|---|---|
| Decision ID | D-1 |
| Decision | May All Open show other companies' titles and names? |
| Current behaviour | Source-verified. `TicketList.aspx.cs` shows All Open only when `HasPermission("ITSM.Queue.View")` is true, and `BindQueue` repeats that check before it calls the service. `TicketListService.GetOpenQueue` sets `OpenOnly` and `BuildQueueQuery` keeps rows whose status is not terminal. There is no `CompanyId` predicate. The grid binds ticket number, title, request type, priority, status, requester name, assignee name, category, branch, and created time. Search also matches `Description`, so a phrase from the description can surface the row even though the description column is not on the grid. The same query builder serves My Queue, which is every non-terminal ticket assigned to the actor, with no company predicate. `GetActiveAgents` returns every active employee. `GetBranches` returns every branch. Detail view is separate: a relationship holder opens any company; `ITSM.Ticket.View` is limited to the actor's current company. Specification assumption: leave the shared-service list unchanged. |
| Business impact | All Open is the shared desk's view of every non-terminal ticket, including On Hold and, under the repository seed, Resolved. Agents who hold the queue permission can see work outside their own company and can reassign it to any active employee. Restricting the list to one company will hide tickets that dispatch assigned by branch and category, because `MST_SupportMapping` has no company column. My Tickets stays the requester's own rows either way. |
| Security impact | High. Holders of `ITSM.Queue.View` can read cross-company titles and employee names and can probe description text through search. The permission scripts assign that key to `ITSM_Agent` and `ITSMAdmin`. Whether those grants are deployed was not executed. Relationship access across companies is a second path and is limited to requester, assignee, and pending approver. A company filter added without D-15 would also hide null-company tickets from the desk while those tickets remain visible to their requester. |
| Available options | A. Keep All Open, My Queue, the agent directory, and the branch filter cross-company for `ITSM.Queue.View` holders. Accept the disclosure in writing. B. Scope All Open to `Tickets.CompanyId` equal to the actor's current company, and leave My Queue and reassignment cross-company. C. Scope All Open, My Queue, approval listing, reassignment targets, and dispatch together, and define a holding area for null `CompanyId`. D. Replace All Open with "tickets whose branch and category match my active support mappings," which is company-agnostic and narrower than all companies. |
| Recommendation | Rule A, as a written acceptance of a shared service desk, unless leadership has a tenant boundary that forbids cross-company titles. Do not implement B on its own: the desk would see a company-limited list and could still be assigned, and could still reassign, across companies. C is a redesign and pulls in D-14 and D-15. This recommendation agrees with the specification's working assumption and is not an approval. |
| Decision owner | Service owner, with IT security concurrence |
| Dependency | D-14, D-15. Section 3 forbids implementing any option by adding `Session["CompanyID"]` |
| Status | Pending |

### D-2

| Field | Content |
|---|---|
| Decision ID | D-2 |
| Decision | May the requester resolve, reassign, or override priority? |
| Current behaviour | Source-verified. `ItsmTicketAccess.CanProcessTicket` returns true when the actor is the requester or the assignee. It does not consult a permission key, a support mapping, or company. `ChangeStatus`, `ReassignTicket`, `OverridePriority`, and `AddComment` all use that gate. The requester therefore can move status, including to Resolved, Closed, On Hold, and Cancelled, and can move Resolved back to In Progress. Reassignment accepts any active employee, or a blank assignee, and writes an audit row with no reason. Priority override requires a non-blank reason and stores it. The requester keeps process rights after the assignee changes, because the check is "requester or assignee," not "current assignee only." The detail page shows the work panel to every viewer of a non-terminal ticket that is not Pending Approval. The server then denies anyone who is neither requester nor assignee. Approve and reject already exclude the requester. Internal notes are refused for a requester who is not also the assignee. Specification assumption: the target says no. Not approved. |
| Business impact | The employee who reported the issue can mark it resolved, send it to someone else, or change its priority. That matches a small desk where the requester also closes the loop. It removes a single accountable assignee. Closure is inside the same gate: the requester can close or reopen without a separate confirmation step. Taking the right away without a replacement actor freezes unassigned tickets. |
| Security impact | High for workflow integrity. The requester can close a ticket the assignee has not finished, can reassign it to any active employee in any company, and can change priority with a reason they write themselves. The work panel is visible to same-company `ITSM.Ticket.View` holders who cannot use it; that is a screen disclosure, and the mutation still fails server-side. Separation of duties already holds for approve and reject. It does not hold for resolve, reassign, or priority. |
| Available options | A. Keep requester or assignee for resolve, reassign, priority override, hold, cancel, close, and reopen. B. Assignee only for resolve, reassign, and priority override. Requester keeps comments, may cancel before work is in progress, and is the only actor who may close or return a resolution. C. Assignee only for every process action, including close and reopen. D. Requester may act only when they are also the assignee. |
| Recommendation | Option B. State the closure and reopen actor in the same ruling, because the question text names resolve, reassign, and priority, while the code uses one gate for close and reopen as well. Do not implement B until D-14 gives a mapped employee a way to take an unassigned ticket. Option A remains available if the service owner wants the requester to keep running their own ticket. This recommendation matches the specification's direction and is not an approval. |
| Decision owner | Service owner, with IT security concurrence |
| Dependency | D-14 for a surviving actor on unassigned tickets. D-5, D-6, D-17, and D-18 use the same actor |
| Status | Pending |

### D-3

| Field | Content |
|---|---|
| Decision ID | D-3 |
| Decision | Must create require `UI.ITSM_CreateTicket.View` and an active employee? |
| Current behaviour | Source-verified. `CreateTicket.aspx.cs` requires `WORKMAN`, `USERID`, and `BRANCH` to be non-null. It does not call `HasPermission` or `RequirePermission`. It does not call `ItsmTenantContext.IsActiveEmployee`. `TicketService.CreateTicket` requires a user-master row, a positive `CompanyId`, and a branch that resolves. It does not read `EmployeeStatus`. The live sign-in path refuses a non-Active employee, so a newly signed-in inactive employee does not get a session. A session that remains after HR sets the employee inactive can still create. `Dristi.Master.cs` `RefreshChromeAfterIdentityChange` runs UI permission visibility and then sets `ITSM_CreateTicket.Visible = true` for every authenticated session. The permission key exists in `Database/Scripts/010_ITSM_Sidebar_Permissions.sql` and in `AnmolDristi/Database/itsm_role_permissions.sql`. The sidebar script's own comment grants it to SuperAdmin and says other roles are assigned separately. Deployed grants were not executed. Specification assumption: recommended yes. Not approved. |
| Business impact | Every employee who can sign in can raise a ticket today, including by opening the page directly. Requiring the permission without first granting it to ordinary employees will stop creation for the people the service is meant to serve. Requiring an active employee at create closes the gap between HR deactivation and session end. It does not change the sign-in rule. |
| Security impact | Medium. Create is an authenticated session check, not a permission check. The menu control that was designed to follow `UI.ITSM_CreateTicket.View` is forced visible afterwards. An inactive employee with a still-valid session can insert a ticket into a company snapshot. Create does not use `Session["BRANCH"]` as the tenant. |
| Available options | A. Require both the permission and `IsActiveEmployee` before create, and approve the role grants in the same ruling. B. Require an active employee only. Leave creation available to every signed-in employee. Keep the permission key for a later restriction. C. Leave the page as it is. |
| Recommendation | Option B for this build. Call `IsActiveEmployee` inside create so a deactivated employee cannot raise a ticket on an old session. Do not turn on `UI.ITSM_CreateTicket.View` until the service owner names the roles that must keep the right and those grants exist. Turning the check on against the current master-page override would hide nothing, because the master page forces the link visible, while the page itself would start refusing. Both the page and the menu have to change together if option A is chosen. This is narrower than the specification's "yes" to both checks. |
| Decision owner | Service owner for who may raise a ticket. IT security for when the permission check is enforced |
| Dependency | Section 3. D-9 does not change who may create |
| Status | Pending |

### D-4

| Field | Content |
|---|---|
| Decision ID | D-4 |
| Decision | How should duplicate ticket numbers be prevented? |
| Current behaviour | Source-verified. `TicketService.GenerateTicketNumber` builds prefix + two-digit year + month + four digits. The prefix comes from `RequestType.Prefix`, trimmed, and cut to two characters. The next sequence is one more than the maximum last-four-digits among tickets of the same `RequestTypeId` whose number starts with that prefix, read with `AsNoTracking` inside the create transaction. There is no `UPDLOCK`, no sequence object, and no unique index. The initial migration indexes foreign keys on `itsm.Tickets` and does not index `TicketNumber`. No SQL script in the repository mentions `TicketNumber`. Two concurrent creates can read the same maximum and insert the same number. Two request types that share a prefix can issue the same number because the read is filtered by `RequestTypeId`. A prefix that trims to one character produces a nine-character number, and the parser ignores values shorter than 10 characters, so the next sequence stays 1. The column is `varchar(10)`. Sequence 10000 does not fit. No duplicate row was queried. Specification assumption: the method is not chosen. Uniqueness is a prerequisite. |
| Business impact | Employees, mail subjects, and audit rows identify a ticket by this number. A duplicate makes "quote the old number" and "duplicate of {number}" point at more than one row. The internal `Tickets.Id` remains unique. The human reference does not. |
| Security impact | Medium for audit integrity. A colliding number weakens the trail that rejection resubmission (D-11) and duplicate marking (D-18) would rely on. It is not a tenant bypass. |
| Available options | A. Unique index on `itsm.Tickets.TicketNumber`, and allocate the next value under a lock in the same transaction, with a bounded retry. Keep prefix + year + month + four digits. Treat 9999 in a month as an operational ceiling the owner is told about. Repair the short-prefix parser so it cannot keep issuing one number. B. A counter row per prefix per month, updated in the create transaction, plus the same unique index. C. Change the public format so the sequence can grow, and backfill a uniqueness rule for numbers already issued. D. Unique index only, with no change to the reader. That turns today's collision into a failed create, and it does not fix a generator that always returns the same nine-character value. |
| Recommendation | Option A. The unique index is the control. The locked allocation is what keeps ordinary creates from failing on that index. Keep the public format. Put the 9999-per-prefix-per-month ceiling in the ruling so it is an accepted limit. Do not choose C unless the service owner needs more than 9999 tickets for one prefix in one month. This is a technical recommendation. It is not an approval to change the database. |
| Decision owner | IT leadership for the method. Service owner to confirm the displayed format stays stable |
| Dependency | D-11 and D-18 consume the number. No dependency on the session keys |
| Status | Pending |

### D-5

| Field | Content |
|---|---|
| Decision ID | D-5 |
| Decision | Is On Hold plus a reason enough, with no extra pending statuses? |
| Current behaviour | Source-verified. The seed, applied only when `itsm.MST_TicketStatus` is empty, includes On Hold with `IsTerminal = false`. `TicketWorkflow` allows New → On Hold, In Progress → On Hold, On Hold → In Progress, and On Hold → Cancelled. `ChangeStatus` does not require a reason for any target except a non-blank `ResolutionText` when the target name is Resolved. The status-change audit row stores the old and new status names and a null detail. There is no hold-reason column. There is no status named Pending Requester, Pending Information, or Pending Vendor. The requester or the assignee can set On Hold. On Hold does not pause a clock, because no clock is stored. Specification assumption: yes for this target. Not approved. |
| Business impact | Agents can pause a ticket without saying whether they are waiting on the requester, a vendor, or internal work. A queue of "waiting on requester" cannot be built honestly. Adding a status per wait reason multiplies the transition map and the training surface. One status plus a mandatory class keeps a single pause and still lets a queue filter on the class. |
| Security impact | Medium for auditability. A pause with no reason leaves a gap in the history of who was accountable while the ticket sat. It does not widen who may see the ticket. |
| Available options | A. Keep the eight seed statuses. Require a class of Requester, Vendor, or Internal on every move to On Hold, and write that class on the audit row. B. Add a status per wait reason and do not add a reason field. C. Leave On Hold reason-free, as the code does today. |
| Recommendation | Option A. Do not add statuses. The storage shape, a column versus the audit detail, is an implementation choice after this ruling and does not need a separate business decision, provided the class is mandatory and is not discarded. Who may set On Hold follows D-2. This recommendation agrees with the specification's working assumption and is not an approval. |
| Decision owner | Service owner |
| Dependency | D-2 for the actor. D-7 for pause-on-hold. D-1 if a waiting queue is also company-filtered |
| Status | Pending |

### D-6

| Field | Content |
|---|---|
| Decision ID | D-6 |
| Decision | Is there a reopen window, and does reopen restart the SLA? |
| Current behaviour | Source-verified. The map allows Resolved → In Progress and Resolved → Closed. It does not allow Closed, Rejected, or Cancelled to leave. `ChangeStatus` does not require a reason when the target is In Progress. It does not compare dates. It does not read or write an SLA column. There is no reopen window in code. If a deployed Resolved row is terminal, both edges are refused before the map is read. That flag was not executed. The actor is whoever passes `CanProcessTicket`, so the requester and the assignee can both reopen and both close. Specification assumption: reopen from Resolved only. Window and SLA effect are not set. |
| Business impact | Without a window, a resolved ticket can return to work at any time, and "resolved" is not a stable completion signal. A window the business has not named cannot be invented here. Resetting an SLA on reopen would let a late ticket look newly on time. Leaving Closed terminal means a wrong confirmation is an operational exception outside the system. |
| Security impact | Medium for workflow integrity. Reopen without a reason, by either the requester or the assignee, rewrites a completed outcome. No cross-company effect beyond D-2. |
| Available options | A. Reopen from Resolved only, at any time, mandatory reason, and if a clock exists extend the due time by the time spent in Resolved. Do not reset the original start. B. Same as A, plus a window the owner names in this ruling, in days. After the window, the only exit is Closed, or a new ticket. C. No reopen. Resolved may go only to Closed. A mistake is a new ticket. D. Leave the current edges: no reason, no window, no SLA effect, requester or assignee. |
| Recommendation | Option A. Ask the service owner to write a day count into the ruling only if they want option B. This register does not propose a number of days. Closed stays terminal. The SLA half of the question is implemented only if D-7 is approved; until then the reason and the actor are the enforceable part. This fills the gap the specification left open. It is not an approval. |
| Decision owner | Service owner |
| Dependency | D-2 for who may reopen and who may close. D-7 for the clock. Deployed `IsTerminal` on Resolved is a check in section 5.3 |
| Status | Pending |

### D-7

| Field | Content |
|---|---|
| Decision ID | D-7 |
| Decision | Are response SLA, stored due time, pause on hold, and one breach mail in scope? |
| Current behaviour | Source-verified. `MST_Priority.SlaResolutionHours` exists. `SlaService.CalculateSla` runs at create. The start uses `VMS_PlantTimings` for the branch id it is given, and the duration counts clock hours to midnight while skipping `MST_Calendar` weekend rows, or Saturday and Sunday when no calendar row exists. The two timestamps are written into the creation email body only. `Tickets` has no SLA columns. There is no response-hour column. On Hold does not pause anything. No job sends a breach mail. Create passes the requester's legacy branch id into SLA and the resolved `MST_Branch.Id` into dispatch. Whether those identifiers match the timings table was not executed. Priority hours in the database were not executed. Specification assumption: yes, after D-5. Not approved. |
| Business impact | Today the due time exists only inside one email. A lost message leaves no due time to report or escalate. Putting the full package in the first build adds a clock, a pause rule, a response target, and a mail. The response target needs a new column or a fraction of the resolution hours. The specification itself treats a true open-to-close calendar as later work. The current calculator is not that calendar. |
| Security impact | Low for access control. Medium for operational accountability: escalation to the assignee's reporting manager discloses the breach to that manager across whatever company the ticket belongs to. The mail must not use a relative link (D-8). |
| Available options | A. In scope as a package: response target, stored resolution start and due, pause on On Hold, one breach mail to the assignee and that employee's reporting manager, no at-risk mail. B. In scope for stored resolution start and due, pause on On Hold, and one breach mail. Defer a separate response SLA. C. Persist the due time only. Defer pause and mail. D. Out of this build. Keep the email-only calculation. |
| Recommendation | Option B. A stored resolution clock, a pause that matches D-5, and one breach mail are the control the email does not provide. A response SLA can follow without changing that ruling. Do not start the work until D-5 and D-6 are ruled, and until the branch-id check in section 5.3 is done in a named environment. Recalculating the due time on priority override should keep the original start. This is narrower than the specification's full package. It is not an approval. |
| Decision owner | Service owner for the clock rules. IT leadership for taking the work into this build |
| Dependency | D-5, D-6, D-8. Section 5.3 branch-id check |
| Status | Pending |

### D-8

| Field | Content |
|---|---|
| Decision ID | D-8 |
| Decision | What absolute base URL should mail links use? |
| Current behaviour | Source-verified. The creation mail sets the link to `~/ITSM/UI/TicketDetail.aspx?id=` plus `Tickets.Id`. No ITSM class reads `Request.Url` or a configuration key for a public origin. This review did not open `Web.config` and did not copy any environment address into this register. There is no session key that holds a site address. Specification assumption: unknown. Requires an owner value. |
| Business impact | Recipients cannot open the ticket from the mail until the message contains an absolute address they can reach. Approving a wrong host sends employees to a site that is not the one they use. More than one host name needs one chosen address, not one per developer machine. |
| Security impact | Medium. The base address decides which site receives the employee's session when they click. It must be an address the organisation operates. It must not be taken from a local configuration file and committed. The link uses the internal numeric id, which is already how the detail page addresses tickets. |
| Available options | A. IT operations names one public HTTPS origin. The application prefixes `ITSM/UI/TicketDetail.aspx?id=` and the id. The origin lives in deployment configuration for that environment. B. Stop putting a link in the mail and quote only the ticket number. C. Leave the `~/` path in the body. |
| Recommendation | Option A. The owner writes the origin in the ruling column of section 6 when they approve this row. This register does not propose an origin. Do not store it in a session key. In the same ruling, mark the specification section 14 recipient list as accepted, reduced, or deferred. Option C leaves the current defect in place. |
| Decision owner | IT operations for the origin. Service owner for whether section 14 mail events are in this build |
| Dependency | D-7 breach mail and any other new mail use the same origin. Independent of D-1 |
| Status | Pending |

### D-9

| Field | Content |
|---|---|
| Decision ID | D-9 |
| Decision | IR approval off, SR and CR Normal on, CR Standard off? |
| Current behaviour | Source-verified mechanism, not executed data. `MST_RequestType` has `RequiresManagerApproval` and `RequiresItHeadApproval`. `TicketService` sends the ticket to Pending Approval when either flag is true, and to New otherwise. Manager is `ReportingManagerCode`. IT Head is the single active `MST_ApprovalConfiguration` row. The repository seed does not insert request types. No request-type seed script was found. `ApprovalConfigurationService.UpdateRequestTypeApproval` lets a holder of `ITSM.ApprovalConfig.Manage` change either flag. That method writes no audit row. There is no change-class column, so one request-type row cannot be both Standard and Normal. Deployed prefixes and flags were not queried. Specification assumption: recommended. Deployed flags unknown. |
| Business impact | The policy is data an administrator can already change. Approving this decision sets the intended data. It does not prove the database matches it. Standard and Normal changes need two request-type rows, or a class field that this decision would have to add. Emergency cannot be represented as "implement first, approve after" without a further rule. Hard-coding the policy in C# would fight the configuration screen. |
| Security impact | Medium. Flag changes alter who must approve before work starts, and they are not written to `TicketAuditLog`. A wrong flag on a change request lets work start with no approver, or blocks work behind an approver the business did not intend. The IT Head is global across companies (D-10). |
| Available options | A. Approve the policy as data: incident flags off; service request both flags on; two change rows, standard off and normal both on. Do not hard-code it. Record flag changes in an audit trail when the screen is next changed. B. Approve one set of flags for a single change request type, and defer Standard versus Normal. C. Leave deployed flags unknown and unchanged until a named environment is read, and rule the policy only after that reading. |
| Recommendation | Option C, then A. Do not treat the specification's recommendation as the deployed state. After a read-only check of the request-type rows in a named environment, set the flags to option A if the rows support two change types. If only one change type exists, option B is the honest ruling until a second row or a class field is approved. This review did not perform that read. |
| Decision owner | Service owner for the policy. ITSM administrator applies the flags after the ruling. IT leadership commissions the read-only check |
| Dependency | D-10 for what "CR Normal on" means when the only authority is the IT Head. D-11 for what rejection of those approvals does |
| Status | Pending |

### D-10

| Field | Content |
|---|---|
| Decision ID | D-10 |
| Decision | Is the IT Head the only change authority, with no CAB? |
| Current behaviour | Source-verified. Change uses the same ticket and the same two flags. There is no CAB table, no CAB member, and no planned-start column. One active IT Head is enforced by a filtered unique index on `MST_ApprovalConfiguration`. `ApproveTicket` refuses the IT Head while a Manager row is still pending. It refuses the requester. Create throws when the requester's reporting manager is the requester. Create still inserts an IT Head row when the requester is the IT Head, and approve then refuses, so the ticket stays Pending Approval. Two rows for the same employee are both inserted when that person is the manager and the IT Head. `SetActiveItHead` and the flag update write no workflow audit. The IT Head is not selected by company, branch, category, or amount. Specification assumption: recommended for this build. Not approved. |
| Business impact | Change control is one named employee plus, when the flag is on, the requester's manager. There is no board, no quorum, and no change window in the system. That is enough for a thin change path. It is not a CAB. A ticket whose requester is the configured IT Head cannot be approved and cannot be moved by `ChangeStatus`, because Pending Approval is not in the transition map. |
| Security impact | High for authorization. One global IT Head can approve a change ticket for every company. That authority is not limited by `Tickets.CompanyId`. A stalled "requester is IT Head" ticket has no administrator override in this build. Replacing the IT Head is an administrative action with no ITSM audit row. |
| Available options | A. IT Head is the only change authority for this build. No CAB, no roster, no meeting. Add create-time guards: refuse when the requester is the IT Head, and keep a single approval row when the manager and the IT Head are the same employee. Audit IT Head changes. B. Same authority, without the new guards. Accept stranded tickets as an operational incident. C. Commission a CAB model: membership, quorum, and a scheduled window. That is a later product, not this build. |
| Recommendation | Option A. It matches the specification's working assumption and closes the two create paths that strand or double-count an approver. Option C stays out of this build unless the service owner rejects A. The global scope of the IT Head should be explicit in the ruling so it is not mistaken for a per-company role. This is not an approval. |
| Decision owner | Service owner, with IT security concurrence on the cross-company authority |
| Dependency | D-9 for which types set the flag. D-1, because the approver sees the ticket through the approval relationship without a company check |
| Status | Pending |

### D-11

| Field | Content |
|---|---|
| Decision ID | D-11 |
| Decision | Is rejection terminal, with a new ticket as the only resubmit? |
| Current behaviour | Source-verified. `RejectTicket` requires a non-blank reason. It marks the acting approval row and every other pending row Rejected. It sets the ticket status to the active status named Rejected, or Cancelled if Rejected is missing. The seed marks both terminal. `ChangeStatus` refuses a terminal status and also refuses any current status that is not in the map. Rejected is not in the map, so there is no screen path back to Pending Approval even if a deployed flag were non-terminal. There is no resubmit method. There is no edit of title, description, or category after create. The employee can open the rejected ticket and raise a new one. The reason is stored on `TicketApprovals.Comments`, which is 500 characters, and on the audit detail. The business layer does not check length. A reason longer than 500 characters fails at save and rolls back. No rejection mail is sent. Specification assumption: recommended. Not approved. |
| Business impact | A rejected request cannot be corrected in place. The employee retypes it and, if they choose, quotes the old number. History of the refusal stays on the old ticket. An in-place resubmit would need editable fields, a reset of approval rows, and a status the map does not have. That is a new product. Employees who expect to fix and resubmit will instead see a dead end. |
| Security impact | Medium for workflow integrity. Terminal rejection stops the requester from approving their own repair of a refused request. The absence of a mail means the requester learns of the refusal only by opening the ticket. An over-long reason fails the transaction rather than being stored in part. |
| Available options | A. Rejection stays terminal. Recovery is a new ticket. The employee may quote the old number in the description. No link column in this build. B. Add an in-place correction: the requester edits the ticket, approvals are created again, and status returns to Pending Approval, with an audit of the edit. C. Add a "copy to new ticket" action that links the new id to the old id. |
| Recommendation | Option A for this build. It matches the code and the specification's working assumption. Tell the service owner that "correction" means a new ticket, because the product cannot edit the original. Quote the old number only after D-4. Option B is the right later decision if employees must keep one number across a refusal. This is not an approval. |
| Decision owner | Service owner |
| Dependency | D-4 for the quoted number. D-9 and D-10 for which tickets can be rejected. D-8 if a rejection mail is added under section 4.4 |
| Status | Pending |

### D-12

| Field | Content |
|---|---|
| Decision ID | D-12 |
| Decision | Is automatic closure authorised? |
| Current behaviour | Source-verified absence. `Ticket.ClosedOn` is documented on the entity as the time of a move to Closed after CSAT or a 72-hour auto-close. `ChangeStatus` sets `ClosedOn` only when a person moves the ticket to a status named Closed. No ITSM job, schedule, or closure service closes a ticket by age. Searches under the ITSM folders found no writer of an automatic close. Specification assumption: no. |
| Business impact | Resolved tickets stay resolved until a person closes them. A comment on the entity is not a rule. Turning on a 72-hour close later will close tickets the requester has not confirmed, including tickets they never saw because no resolve mail exists today. |
| Security impact | Low while the job does not exist. An unauthorised job would close tickets without an actor the requester chose. Any future job needs a named actor, a clock, and an exception path written into a new ruling. |
| Available options | A. Not authorised. Do not build a job. B. Authorise a job. The ruling must name the age, the status it may close, the recorded actor, and the exception path. Those values are not in the current source. |
| Recommendation | Option A. Ignore the 72-hour sentence on the entity until a later decision names the rule. This agrees with the specification's working assumption and is not an approval. |
| Decision owner | Service owner |
| Dependency | D-2 and D-6, because a job would compete with requester confirmation. D-13 if a score were required before a job could close |
| Status | Pending |

### D-13

| Field | Content |
|---|---|
| Decision ID | D-13 |
| Decision | Is CSAT required to close? |
| Current behaviour | Source-verified. `Tickets.CsatScore` and `CsatFeedback` exist. The detail page displays them when a score is present. No ITSM method assigns `CsatScore`. `ChangeStatus` to Closed checks the transition and the actor. It does not read the score. Specification assumption: no. |
| Business impact | Closure is not blocked on a survey. Requiring a score needs a form that does not exist and will stop closure whenever the requester does not answer. Leaving the columns unused preserves them for a later decision. |
| Security impact | None for access control. A mandatory score would add a requester action before Closed. It would not change who may view the ticket. |
| Available options | A. CSAT is not required. Leave the columns unused. Do not block Closed. B. Require a score from the requester before Closed. Specify the scale and what happens if they never answer. C. Capture an optional score and still allow close without it. |
| Recommendation | Option A for this build. Option C is a later enhancement and still needs a writer that does not exist. This agrees with the specification's working assumption and is not an approval. |
| Decision owner | Service owner |
| Dependency | D-12. D-2 for who is allowed to close if a score is ever added |
| Status | Pending |

### D-14

| Field | Content |
|---|---|
| Decision ID | D-14 |
| Decision | Must a support-group entity exist, or is branch + category + one agent enough? |
| Current behaviour | Source-verified. `MST_SupportMapping` stores one `SupportAgentCode` per branch plus an optional category, subcategory, and item. `DispatchService` walks item, subcategory, category, then branch default, and returns the first code or null. A null assignee is allowed on the ticket. There is no group table and no team-lead role. `ItsmTicketAccess` says support mapping is create-time dispatch, not a live access list. Reassignment does not check the mapping. Multiple active rows at the same specificity are possible; the query uses `FirstOrDefault` with no order, so the chosen agent is not deterministic. There is no screen to maintain mappings. Specification assumption: one agent is enough for this build. |
| Business impact | One person is accountable for a branch and taxonomy cell. A shared queue where any of several peers may own the work is not representable as a group. Unassigned tickets, which dispatch produces when no row matches, have no team owner. If D-2 removes the requester, those tickets need a defined taker or they freeze. |
| Security impact | Medium. Dispatch and reassignment are not company-scoped, so the single agent can sit in another company from the requester. That is consistent with D-1 option A and inconsistent with a tenant-isolated desk. Mapping rows are changed by script, outside the ITSM audit log. |
| Available options | A. No group entity. Keep one mapped agent. Add one rule on the existing table: an active employee who is the mapped agent for that branch and category may take an unassigned ticket. Breach mail, if D-7 is approved, goes to that employee's reporting manager. B. No group entity and no take-ownership rule. Only the requester can move an unassigned ticket, which forces D-2 to stay at today's requester rights for that case. C. Add a support-group entity and assign the ticket to the group. That is a new table and a new maintenance screen. |
| Recommendation | Option A. It keeps the specification's "one agent is enough" and supplies the actor D-2 needs. Do not treat unordered duplicate mapping rows as a group. Option C waits for a later decision. This is not an approval. |
| Decision owner | Service owner |
| Dependency | D-2. D-1 if dispatch must become company-scoped. D-7 uses the reporting manager because there is no team lead |
| Status | Pending |

### D-15

| Field | Content |
|---|---|
| Decision ID | D-15 |
| Decision | What happens to tickets whose `CompanyId` is null? |
| Current behaviour | Source-verified. The column is nullable. The migration that added it states that it does not backfill, because the requester's current company is not proof of the company at create time. Create now refuses a requester with no positive company. All six mutations call `RequireRecordedTenant` and return the error that mutations are blocked until tenant ownership is remediated. View by requester, assignee, or pending approver still succeeds when company is null. `ITSM.Ticket.View` does not, because the permission path requires a company match. All Open and My Queue still list the ticket when it is not terminal. Audit insert uses `CompanyId.Value` and is unreachable for these tickets because mutation is refused first. No count of null rows was queried. Specification assumption: they stay frozen. No backfill is proposed. |
| Business impact | Historical tickets without a company can be read by the people on the ticket and can appear on the shared queues. Nobody can change status, assignee, priority, or comments through the application. They will not enter the new workflow. A backfill from today's HR company would stamp a tenant that may be wrong and would then unlock mutation under that tenant. |
| Security impact | High if backfilled from current HR data: the ticket would become mutable under a company that was not evidenced. Leaving them listed in All Open continues the D-1 disclosure for rows that have no tenant badge other than "Tenant not recorded" on detail. Freezing them is the isolation control the code already applies. |
| Available options | A. Leave them frozen. Do not backfill. Exclude them from any new processing. Visibility follows D-1. B. Remediate only rows where an owner can show the company at create time from evidence outside the current HR row, then set `CompanyId` in a separately authorised data change. C. Backfill from the requester's current `MST_UserMaster.CompanyId` and unblock mutation. |
| Recommendation | Option A. Option B remains available as its own authorised data exercise and is not part of the application build. Option C is rejected by this recommendation because the migration comment and the entity comment both say current HR company is not historical ownership. This agrees with the specification's working assumption and is not an approval. |
| Decision owner | IT leadership and the data owner. An implementer does not make this ruling |
| Dependency | D-1 for whether frozen rows remain on All Open. Section 3: do not infer company from `Session["BRANCH"]` or from a new session key |
| Status | Pending |

### D-16

| Field | Content |
|---|---|
| Decision ID | D-16 |
| Decision | Is a department snapshot required on the ticket? |
| Current behaviour | Source-verified. `MST_UserMaster.DepartmentId` exists. `Tickets` has no department column. The list and detail DTOs do not select a department. The live sign-in path copies `DepartmentId` into `Session["U_SITE"]`. ITSM create, list, and detail do not read `U_SITE`. The department shown by HR today can change after the ticket exists. Specification assumption: no snapshot. Show the requester's current department. |
| Business impact | Reports cannot say which department raised the ticket. Showing the live department will change when the employee transfers, and it will read as history. A snapshot preserves the value at create and needs a column the specification's first build avoids. |
| Security impact | Low. Department is not the tenant. `CompanyId` is. Using department as an access boundary would be a new authorization model and is not proposed. |
| Available options | A. No snapshot and no department on the ticket screens. B. No snapshot, and display the requester's current department, labelled as current HR data. C. Add a department snapshot at create and show that frozen value. |
| Recommendation | Option A. This differs from the specification's working assumption, which is to show the current department. Displaying live HR data on a historical ticket will be read as the department that raised it. Wait for a report owner to require C. This is not an approval. |
| Decision owner | Service owner. A later snapshot also needs the HR data owner |
| Dependency | None for option A. Independent of D-15 |
| Status | Pending |

### D-17

| Field | Content |
|---|---|
| Decision ID | D-17 |
| Decision | Minimum length of a resolution summary? |
| Current behaviour | Source-verified. `ChangeStatus` refuses Resolved when `ResolutionText` is null or whitespace. It trims and stores the text, and sets `ResolvedOn`. The status audit row passes null as the detail, so the summary is not copied into history. The column is `nvarchar(max)`. There is no minimum length and no maximum length in the business layer. Audit `Detail`, if it were used, is 1000 characters and `AddAudit` truncates to that length. Specification assumption: non-blank. No minimum until set. |
| Business impact | "Resolved" can be a single character. A long minimum will reject genuine short outcomes. The history of the ticket records that the status became Resolved and does not record the words. A later reader of the audit cannot see the outcome if the ticket text is edited by a path outside this application. The application does not expose an edit of `ResolutionText` except by resolving again, which the map does not allow from Resolved. |
| Security impact | Medium for auditability. The business fact of the resolution is on the ticket row and not on the audit row. Silent truncation would make the audit copy disagree with the ticket. The current code does not truncate the ticket column. |
| Available options | A. Non-blank after trim. No character minimum. When a future change copies the summary onto the audit row, refuse a summary longer than 1000 characters instead of truncating it. B. Set a minimum in this ruling. The owner writes the number. Also apply the same refusal at 1000 characters if the audit copy is added. C. Leave the audit detail null. Keep non-blank on the ticket only. |
| Recommendation | Option A. Do not invent a minimum length. Copying the summary into the audit detail is a technical recording of a rule the code already enforces, and it should refuse over-length text rather than cut it. This agrees with the specification on length and is not an approval. |
| Decision owner | Service owner for any minimum length. IT leadership if the audit copy's 1000-character ceiling is accepted |
| Dependency | D-2 for who may write the summary. D-18 uses the same field |
| Status | Pending |

### D-18

| Field | Content |
|---|---|
| Decision ID | D-18 |
| Decision | May an agent mark a ticket duplicate without a link column? |
| Current behaviour | Source-verified absence of a link. `Tickets` has no parent or duplicate foreign key. Nothing in `TicketUpdateService` checks that a resolution mentions another ticket. An agent who can resolve can type any sentence, including "Duplicate of {number}". The application does not verify that the number exists, is unique, or is a ticket the agent may view. Specification assumption: yes, by text in the summary, if accepted. |
| Business impact | Duplicates can be closed with a sentence and without a new table. They cannot be listed as a set, and a wrong number is not caught. While D-4 is open, the sentence can point at two rows. A link column would make the relationship reportable and would need a rule for cross-company targets. |
| Security impact | Medium if the text names a ticket the writer should not have seen. The sentence is visible to everyone who can view the resolved ticket. A link column would need the same company rule as D-1, or it becomes a pointer across tenants. |
| Available options | A. Allow text in the resolution summary. No link column. Do so only after D-4. Accept that duplicates are not a report. B. Refuse duplicate marking until a parent column exists, and do not add that column in this build. C. Add a nullable parent-ticket id, validated against a ticket the actor may view, in a later build. |
| Recommendation | Option A, gated on an approved D-4. Do not add a column in this build. The agent must be a process actor under D-2. This agrees with the specification's working assumption and is not an approval. |
| Decision owner | Service owner |
| Dependency | D-4, D-2, D-17. D-1 if a later link may cross companies |
| Status | Pending |

---

## 7. Critical security decisions

These rows change who can see another company's data, who can move a ticket, or who can authorise a change. They need IT security concurrence as well as the service owner. Pending means the current code stands and the target must not quietly "correct" it.

| ID | Current control | What an unreviewed change would do |
|---|---|---|
| D-1 | All Open and My Queue are cross-company for `ITSM.Queue.View`. Detail by permission is company-scoped. Detail by relationship is not | A partial filter would look like isolation and leave search, reassignment, or My Queue open |
| D-2 | Requester retains resolve, reassign-to-anyone, priority override, close, and reopen after the assignee changes | Leaving it in place keeps a separation-of-duties gap the approval path does not have. Removing it without D-14 freezes unassigned work |
| D-3 | Any session with `WORKMAN`, `USERID`, and `BRANCH` can create. The menu item is forced visible. Active status is enforced at the live sign-in, not at create | Enabling the permission without grants locks out employees. Skipping the active check leaves a deactivated session able to create |
| D-10 | One global IT Head, not company-scoped, can approve every company's change. Requester-as-IT-Head stalls. Flag and IT Head edits are unaudited | Treating that person as a per-company role would overstate the control. Adding a CAB by inference would invent an authority model |
| D-15 | Null `CompanyId` cannot be mutated and must not be backfilled from current HR company | A backfill would authorise mutation under an unevidenced tenant |
| Section 3 | Actor is `WORKMAN`. Company is the ticket snapshot plus, for one view path, a live user-master read. `BRANCH` is a presence check of the legacy branch id | Adopting `Session["BranchID"]` or `Session["CompanyID"]` would break sign-in or would enforce a stale, wrong key |

Resolution and closure authorization sits inside D-2. On Hold authorization sits inside D-2 and D-5. Notification content sits in section 4.4 and D-8. Ticket-number integrity sits in D-4.

---

## 8. Crosswalk to the older decision list

`ITSM_AS_IS_Findings_and_Risks.md` section 10 numbered a different list. Use this table so a ruling on "D-1" names the question. This register does not edit that file.

| This register | Older list in the as-is findings | Relationship |
|---|---|---|
| D-1 All Open across companies | Older D-3 | Same subject, different identifier |
| D-2 Requester resolve, reassign, priority | Older D-4, and part of older D-5 | This register also forces the close and reopen actor into the ruling |
| D-4 Number uniqueness | Not in the older list of ten | New in the specification |
| D-7 SLA in scope | Older D-7 | Same subject |
| D-13 CSAT required to close | Part of older D-8, which also asked about attachments | Attachments are not one of these 18 |
| D-14 One agent or a group | Older D-5 and older D-6 | This register does not define an "agent hierarchy" beyond option A |
| D-15 Null CompanyId | Older D-10 | Same subject |
| Not in these 18 | Older D-1, is Resolved terminal? | Technical check in section 5.3. Seed says no. Deployed value was not read |
| Not in these 18 | Older D-2, is New → Resolved allowed? | Source-verified: the map does not allow it. `ChangeStatus` refuses it. No owner question remains in section 18 |
| Not in these 18 | Older D-9, must the assignment matrix be administratively maintainable? | Still unanswered. Mappings are a script. This register does not promote that into a nineteenth workflow decision |

---

## 9. Recommended decision sequence

Rule in this order. A later row may be discussed early. It should be signed after the rows it depends on.

| Order | ID | Ruling to record | Unblocks |
|---|---|---|---|
| 1 | D-1 | Shared desk, or a named isolation package | Queue behaviour |
| 2 | D-15 | Frozen, no HR backfill, or a separate evidenced remediation | Null-company handling beside D-1 |
| 3 | D-14 | One mapped agent, plus who may take an unassigned ticket | A safe D-2 |
| 4 | D-2 | Requester process rights, including close and reopen | Actor rules on every mutation |
| 5 | D-3 | Active employee now; permission check only with a grant plan | Create |
| 6 | D-5 | On Hold plus a mandatory reason class | Status model, later pause |
| 7 | D-11 | Terminal rejection and a new ticket | Recovery path |
| 8 | D-10 | IT Head only, with the two create-time guards | Change authority |
| 9 | D-9 | Policy after a read-only look at request-type rows | Flags as data |
| 10 | D-6 | Reopen from Resolved with a reason; no invented day count; do not reset a future clock | Confirmation loop |
| 11 | D-17 | Non-blank summary; no silent audit cut | Resolve quality |
| 12 | D-12 | No automatic closure | Exclusion |
| 13 | D-13 | No CSAT gate | Exclusion |
| 14 | D-18 | Text only, after D-4 | Duplicate handling |
| 15 | D-16 | No department on the ticket | Exclusion |
| 16 | D-7 | Stored resolution clock, pause, one breach mail; response SLA deferred | SLA work |
| 17 | D-4 | Unique index and locked allocation; format unchanged | Trustworthy numbers |
| 18 | D-8 | An owner-supplied origin, plus a yes or no on the section 14 mail list | Clickable mail |

D-4 and D-8 can be prepared in parallel with orders 1 through 5. They should still be signed before build steps that quote the number or send a link.

Confirm section 3 in the same meeting: ITSM continues to use `WORKMAN`, `USERID`, and `BRANCH` as they work today, and does not gain `BranchID` or `CompanyID` session keys.

---

## 10. Owner approval register

No names are recorded. No row is approved. The owner role writes the chosen option, the date, and a status of Approved or Deferred. A Deferred ruling states the option that applies while it waits. An empty ruling is Pending and blocks whatever section 5 says it blocks.

| Decision ID | Owner role | Recommended option | Owner ruling | Date | Status |
|---|---|---|---|---|---|
| D-1 | Service owner, IT security concurrence | A. Shared-service All Open, disclosure accepted in writing | | | Pending |
| D-2 | Service owner, IT security concurrence | B. Assignee processes; requester comments, cancels early, and confirms or returns resolution | | | Pending |
| D-3 | Service owner; IT security for the permission check | B. Active employee at create. Permission check waits for a grant plan | | | Pending |
| D-4 | IT leadership; service owner confirms the format | A. Unique index and locked allocation. Format unchanged. Ceiling 9999 per prefix per month | | | Pending |
| D-5 | Service owner | A. On Hold plus mandatory reason class. No new statuses | | | Pending |
| D-6 | Service owner | A. Reopen from Resolved with a reason. No day count unless the owner writes one. Do not reset the clock | | | Pending |
| D-7 | Service owner; IT leadership for scope | B. Stored resolution SLA, pause, one breach mail. Response SLA later | | | Pending |
| D-8 | IT operations for the origin; service owner for section 14 | A. One public HTTPS origin supplied by the owner. Section 14 marked accepted, reduced, or deferred | | | Pending |
| D-9 | Service owner; administrator applies data after a read-only check | C then A. Do not hard-code. Do not assume deployed flags | | | Pending |
| D-10 | Service owner, IT security concurrence | A. IT Head only. Create-time guards. No CAB | | | Pending |
| D-11 | Service owner | A. Terminal. New ticket is the correction path | | | Pending |
| D-12 | Service owner | A. Automatic closure is not authorised | | | Pending |
| D-13 | Service owner | A. CSAT does not block closure | | | Pending |
| D-14 | Service owner | A. One mapped agent. Mapped agent may take an unassigned ticket | | | Pending |
| D-15 | IT leadership and data owner | A. Stay frozen. No backfill from current HR company | | | Pending |
| D-16 | Service owner | A. No snapshot and no live department on the ticket | | | Pending |
| D-17 | Service owner | A. Non-blank. Refuse rather than truncate if the summary is copied to audit | | | Pending |
| D-18 | Service owner | A. Text in the summary, only after D-4. No link column | | | Pending |
| Section 3 | IT leadership | Keep `WORKMAN`, `USERID`, and `BRANCH`. Do not add `BranchID` or `CompanyID` session keys | | | Pending |
| Section 4.4 | Service owner | Specification section 14 mail list accepted, reduced, or deferred. Ruled with D-8 | | | Pending |

---

## 11. Implementation dependencies

These map the specification's recommended build order to the rulings above. They are not an instruction to start.

| Specification order | Work | Rulings required first | Note |
|---|---|---|---|
| 0 | Review only | This register | This file is that review. It does not authorise order 1 |
| 1 | Unique ticket number, encode detail output, confirm deployed `IsTerminal` | D-4 for the number. Section 5.3 for the flag | Encoding of detail labels is a defect fix already identified in the specification. It is not one of the 18. It still needs its own implementation instruction |
| 2 | One policy method for approve, reject, and status change. Hold reason and reassignment reason on the audit detail | D-5, D-2, D-11, D-10 | Reason text is useless if the actor set then changes under a late D-2 |
| 3 | Restrict process actions | D-2 and D-14 | Take-ownership for the mapped agent lands here, or unassigned tickets freeze |
| 4 | Mails for approve, reject, resolve, reassign, and requester hold. Absolute link | D-8 and the section 14 ruling. D-2 for who the actors are | Creation mail already exists and should gain the same absolute link |
| 5 | Queue filters for unassigned, resolved, and waiting reason | D-5 for the reason. D-1 before any filter adds company | Team-queue behaviour follows D-14 option A |
| 6 | Persist SLA start and due. One breach mail | D-7, D-6, D-5, D-8, and the branch-id check | Response SLA stays out under the recommended option B |
| 7 | Attachments, CSAT, auto-close, CAB dates, support-group entity, extra approval dimensions | D-12, D-13, D-10, D-14 as explicit "not in this build" | Each stays deferred until its own later ruling reverses the exclusion |

Further constraints on any later build, restated so they are not rediscovered as improvements:

- New behaviour stays in `~/ITSM/UI/`, `~/ITSM/BLL/`, and `~/ITSM/DAL/`.
- Header status continues to change only through the ticket services. UI does not gain SQL.
- A null `CompanyId` stays immutable unless D-15 option B is separately authorised.
- `CanApproveTicket` stays separate from `CanProcessTicket`.
- Session identity stays on `WORKMAN`. Company filtering, if D-1 ever chooses it, uses `Tickets.CompanyId` and a fresh user-master read, not a new session key.
- Deployed `IsTerminal` for Resolved is recorded from a named environment before anyone demonstrates close or reopen.
- This review did not verify that environment.

---

## 12. Source inspected and limits

Read for this register:

- `ITSM_TARGET_OPERATING_WORKFLOW_SPECIFICATION.md` section 18 and the sections that define each assumption
- `AnmolDristi/ITSM/BLL/ItsmTicketAccess.cs`, `TicketWorkflow.cs`, `TicketUpdateService.cs`, `TicketService.cs`, `TicketListService.cs`, `TicketDetailService.cs`, `SlaService.cs`, `DispatchService.cs`, `ItsmTenantContext.cs`, `ApprovalConfigurationService.cs`
- `AnmolDristi/ITSM/UI/CreateTicket.aspx.cs`, `TicketList.aspx.cs`, `TicketDetail.aspx.cs`, `TicketDetail.aspx`, `ApprovalConfig.aspx.cs`
- `AnmolDristi/ITSM/DAL/Entities/Ticket.cs`, `RequestType.cs`, `SupportMapping.cs`, `TicketApproval.cs`, `TicketAuditLog.cs`, `ApprovalConfiguration.cs`, `UserMaster.cs`, `Priority.cs`, `Category.cs`
- `AnmolDristi/ITSM/DAL/Migrations/Configuration.cs`, `202608120550515_InitialItsmSchema.cs`, `202608210700000_AddTicketCompanyId.cs`, `202608240451107_AddApprovalConfiguration.cs`
- `AnmolDristi/BasePage.cs`, `Dristi.Master.cs` `RefreshChromeAfterIdentityChange`, `login.aspx.cs` `ProcessSignIn`
- Permission scripts `Database/Scripts/010_ITSM_Sidebar_Permissions.sql`, `AnmolDristi/Database/itsm_role_permissions.sql`, `AnmolDristi/Database/itsm_phase1b_permissions.sql`
- ADR `docs/04-adrs/0001-itsm-architecture-strategy.md` for the session model it names
- `ITSM_AS_IS_Findings_and_Risks.md` section 10, only to record the identifier collision

Not done:

- SQL execution, UAT, LIVE, and any count of null `CompanyId`, duplicate ticket numbers, request-type flags, or `IsTerminal`
- A browser pass
- Reading `Web.config` or copying an environment URL
- Any edit to existing documents, source, or database files
- Any staging, commit, or push

The working tree already contained changes before this file was added, including the untracked ITSM review documents and a local configuration change. This review added only `ITSM_BUSINESS_DECISION_REGISTER.md`.
