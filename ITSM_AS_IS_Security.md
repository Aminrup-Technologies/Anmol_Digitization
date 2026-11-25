# ITSM AS-IS Security Review

**Document:** 4 of 6 — Security
**Mode:** Read-only. No authentication, authorization, or database test was executed.
**Date:** 2026-11-12

---

## 1. Executive Summary

ITSM inherits the monolith's session model and RBAC from `AnmolDristi.BasePage`, then
adds a **tenant-scoped authorization model of its own** in `ItsmTicketAccess`. That
model is the strongest part of the module: authorization decisions are derived from
stored relationships (requester, assignee, pending approval) plus an explicit
permission-plus-tenant check, and they are made **server-side in the BLL**, not by
hiding controls.

Three material weaknesses were verified:

1. **Stored XSS in the ticket detail page.** `lblDescription.Text = dto.Description`
   renders unencoded user input. `Label.Text` is written to the response verbatim by
   Web Forms — it does not HTML-encode. Title, resolution text, and CSAT feedback are
   rendered the same way. Comments and approval comments **are** encoded, which shows
   the correct pattern was known (`SEC-01`).
2. **A permission-holding agent cannot process tickets, and a requester can.**
   `CanProcessTicket` is `requester OR assignee` only. `ITSM.Ticket.View` confers
   *view*, and there is no "authorized agent" processing path. This is both a
   functional gap (reviewer item `GAP-10`) and an authorization asymmetry: the person
   who raised the ticket can resolve it, while a colleague with ticket permissions
   cannot (`SEC-02`).
3. **Cross-company ticket metadata is visible in queues by design.** Queue queries
   carry no `CompanyId` predicate. Detail access *is* tenant-scoped, so a viewer
   cannot open the ticket, but the grid lists ticket number, title, requester,
   assignee, category, branch and priority across all companies (`SEC-03`).

One widespread pattern is **safe** and should be recorded as such: every mutation
authorization decision is re-derived server-side from the loaded ticket row, and the
actor identity always comes from `Session["WORKMAN"]`, never from a form field or
query string.

---

## 2. Session and Authentication Surface

### 2.1 Two overlapping mechanisms

| Mechanism | Where | Behaviour |
|---|---|---|
| `BasePage.OnInit` → `EnsureSessionValid()` | `AnmolDristi/BasePage.cs` | If `Session["LoginToken"]` parses as a GUID: heartbeat `SessionHelper.UpdateHeartbeat` at most once/60 s; every 5 minutes `SessionHelper.IsSessionActive(token)`, else `RedirectToLogin()` |
| Per-page guard | All four ITSM pages | `Session["WORKMAN"]`, `["USERID"]`, `["BRANCH"]` must be non-null, else redirect to `~/login.aspx` |

Both are defensive and duplicated. Observations:

- `EnsureSessionValid` **returns early when `Session["LoginToken"]` is null**, so a
  session that has `WORKMAN`/`USERID` but no `LoginToken` is not heartbeated or
  soft-validated. The per-page guard still passes. Whether such a session can exist
  depends on `login.aspx`, which is outside this module's scope (`SEC-08`,
  Clarification Required).
- `EnsureSessionValid` deliberately swallows all exceptions ("NEVER logout on
  transient failures"). That favours availability over revocation: a database outage
  suspends session invalidation.
- `RedirectToLogin()` clears the session, abandons it, expires the
  `ASP.NET_SessionId` cookie, and completes the request. This is correct.
- ITSM never checks the actor's `EmployeeStatus` on page load. `ItsmTenantContext`
  exposes `IsActiveEmployee`, and `TicketService`/`Reassignment` check it, but
  `TicketDetail`/`TicketList`/`CreateTicket` do not call it. A deactivated employee
  with a live session can still reach and use the pages (`SEC-07`).

### 2.2 Identity source

The actor identity is **always** `Session["WORKMAN"]`:

- `TicketDetail.GetActorCode()` → `Session["WORKMAN"]`
- `TicketList.BindQueue()` → `Session["WORKMAN"]`
- `CreateTicket.btnSubmit_Click` → `Session["WORKMAN"]`

No code path reads an actor code, company ID, or role from a query string, hidden
field, or cookie. **No browser-supplied identity bypass was found.** This is the
single most important positive security property of the module.

---

## 3. Authorization Model

### 3.1 Permissions in use

| Permission key | Enforced at | Effect |
|---|---|---|
| `ITSM.Ticket.View` | `TicketDetail.aspx.cs` → `ItsmTicketAccess.CanViewTicket` | View any ticket **in the actor's company** |
| `ITSM.Queue.View` | `TicketList.aspx.cs` `ApplyTabVisibility` + `IsTabAuthorized` | My Queue, All Open |
| `ITSM.Approval.View` | same | Pending Approvals tab |
| `ITSM.ApprovalConfig.Manage` | `ApprovalConfig.aspx.cs` | Change IT Head / approval flags |
| `UI.ITSM_Module.View`, `UI.ITSM_TicketList.View`, `UI.ITSM_CreateTicket.View`, `UI.ITSM_ApprovalConfig.View` | Sidebar/menu only | **Cosmetic** navigation visibility |

Permission resolution (`BasePage.RebuildSessionPermissions`) reads
`dbo.UserRole → dbo.RolePermission → dbo.Permission` filtered on `IsObsolete = 0`,
caches the set in `Session["PH_UserPermSet"]`, and is refreshed only via
`RefreshUserPermissions()`. Consequence: **a permission granted or revoked during a
live session does not take effect until the session rebuilds** (`SEC-06`).

Role assignments are defined in three SQL scripts under `AnmolDristi/Database/`:
`ITSM_Agent` receives `ITSM.Ticket.View`, `ITSM.Queue.View` and the UI keys;
`ITSMAdmin` receives those plus `ITSM.Approval.View`,
`ITSM.ApprovalConfig.Manage`, `UI.ITSM_ApprovalConfig.View`. `ITSM_Agent`
intentionally does **not** receive approval-view or config-manage. There is **no
`ITSM_Manager` role** in the scripts, despite a code comment referring to one.

### 3.2 The three authorization predicates

```csharp
CanViewTicket   = requester || assignee || hasPendingApproval
                  || (hasTicketViewPermission && ticket.CompanyId == authorizedCompanyId)

CanProcessTicket = requester || assignee          // no permission parameter at all

CanApproveTicket = hasPendingApproval && actor != requester
```

`TicketDetailService.GetTicketDetail` calls `CanViewTicket` and returns `null` when it
fails, which the page renders as "Ticket not found or you do not have access" —
correct behaviour for avoiding ticket-ID enumeration.

`TicketUpdateService` re-checks `CanProcessTicket` / `CanApproveTicket` **inside** the
mutation transaction against the freshly loaded row. It does not trust the page's
earlier decision. This is a genuine defence-in-depth layering of UI gate + BLL gate.

### 3.3 Verified authorization by operation

| Operation | UI gate | BLL gate | Tenant gate | Notes |
|---|---|---|---|---|
| View ticket | session + `CanViewTicket` | `CanViewTicket` | permission path only | Requester/assignee/approver bypass tenant check |
| Create ticket | session only | none beyond requester's company | requester `CompanyId > 0` | **No `ITSM.*` permission check** (`SEC-04`) |
| Change status | `pnlWorkActions` visibility | `CanProcessTicket` | `RequireRecordedTenant` | Requester allowed |
| Reassign | panel visibility | `CanProcessTicket` | required | Target need only be active employee |
| Override priority | panel visibility | `CanProcessTicket` | required | Requester allowed |
| Add comment | panel visibility | `CanProcessTicket` | required | Internal note restricted |
| Approve / Reject | `dto.CanApprove` | `CanApproveTicket` | required | Requester blocked |
| Configure IT Head | `ITSM.ApprovalConfig.Manage` | `EnsureActiveActor` | none | Actor must be active |

---

## 4. CompanyID Isolation

### 4.1 The model

`Tickets.CompanyId` is an **immutable snapshot** of
`MST_UserMaster.CompanyId` taken at creation. It is explicitly *not* re-derived from
the requester's current HRMS record, and `ItsmTenantContext` carries a comment
forbidding its use as a ticket authorization gate. This is a coherent and defensible
design: tenant ownership must not change when HR moves an employee.

### 4.2 Isolation matrix

| Surface | CompanyId used? | Enforcement | Assessment |
|---|---|---|---|
| Create | written from `requester.CompanyId` | refuses null/<=0 | Strong |
| View (requester/assignee/approver) | not compared | relationship-based | Intended: relationships cross companies |
| View (permission path) | compared | `ticket.CompanyId == authorizedCompanyId` | Strong |
| Mutate | required non-null | `RequireRecordedTenant` | Strong but absolute |
| Audit row | copied from ticket | `NOT NULL` column | Consistent |
| Queue listing | **not used** | none | **Cross-company metadata visible** |
| Detail tenant badge | ticket snapshot | display only | Read-only |

### 4.3 Findings

- `SEC-03` — **Queue metadata is cross-tenant.** `BuildQueueQuery` applies no
  `CompanyId` predicate; `GetAgentQueue`, `GetOpenQueue`, `GetApprovalQueue` and
  `GetMyTickets` all share it. The code comment states this is deliberate
  ("shared-service queues"). Whether listing another company's ticket titles is
  acceptable is an **owner risk-acceptance decision**, not a code defect. It is
  classified `Configuration Required (policy)`. Note the UI *does* display Title,
  Requester and Assignee, so the disclosure is not limited to a count.
- `SEC-05` — **Historical tickets with `NULL CompanyId` are frozen.** All six
  mutations return "This ticket has no recorded tenant (CompanyId). Mutations are
  blocked until tenant ownership is remediated." The `CompanyId` column was added on
  2026-08-21 with an explicit decision **not** to backfill, because current requester
  company is not provable historical ownership. The code therefore implements a
  fail-closed policy correctly. The *operational* consequence — pre-migration tickets
  cannot be progressed by anyone — is a business risk, not a security weakness.
  Backfill is explicitly out of scope for this review and for the prior Stage 2 plan.
- No FK from `Tickets.CompanyId` to `MST_Company` means an invalid tenant value can
  exist. The detail join degrades to a "Company N" badge rather than failing, so a
  bad value is displayed rather than rejected (`DB-14`).

---

## 5. Output Encoding and XSS

### 5.1 Verified rendering table (`TicketDetail.aspx` / `.aspx.cs`)

| Field | Rendering | Encoded? |
|---|---|---|
| `Description` | `lblDescription.Text = dto.Description` | **No** |
| `Title` | `lblTitle.Text = dto.Title` | **No** |
| `ResolutionText` | `lblResolution.Text = dto.ResolutionText` | **No** |
| `CsatFeedback` | `lblCsatFeedback.Text = ...` | **No** |
| `RequesterName`, `AssigneeName`, `BranchName`, `CategoryName`, `ItemName`, `ImpactName`, `UrgencyName` | `Label.Text` | **No** |
| `CommentText` | `<%# HttpUtility.HtmlEncode(Convert.ToString(Eval("CommentText"))) %>` | **Yes** |
| Approval `Comments` | `<%# ... HttpUtility.HtmlEncode(Eval("Comments") as string) ... %>` | **Yes** |
| Audit grid `OldValue`/`NewValue`/`Detail` | `asp:BoundField` | `BoundField.HtmlEncode` defaults **true** |
| Audit grid `ActorName` | `asp:BoundField` | encoded (BoundField default) |

**Technical basis for "No":** `System.Web.UI.WebControls.Label` writes `Text` to the
response via `HtmlTextWriter.Write` without HTML-encoding. Assigning to `.Text` is
therefore *not* a mitigation. The only fields that are genuinely safe are those that
pass through `HttpUtility.HtmlEncode` or `BoundField`'s default encoding.

### 5.2 Exposure

| Field | Who can set it | Who is exposed |
|---|---|---|
| `Title` | any authenticated employee (create form, `MaxLength=200`) | every viewer of the ticket |
| `Description` | any authenticated employee (multiline, unbounded) | every viewer |
| `ResolutionText` | requester **or** assignee | every viewer |
| `CommentText` | requester/assignee (encoded — safe) | — |
| `CsatFeedback` | **nobody** (no writer) | unreachable today |

`Description` is the strongest vector: unbounded `nvarchar(max)`, entered by any
employee, rendered raw to IT staff and approvers. This is a **stored XSS** condition.
It is classified `Architectural Gap` because there is no systematic,
encoding-by-default rendering boundary — the page mixes encoded and unencoded output
by hand.

The prior Stage 2 plan asserts that `Label` "auto-encodes `<` and `>` only". That is
incorrect: `Label` performs **no** encoding. The practical severity assessment does
not change (raw output is raw output), but the stated rationale should not be relied
on.

`CreateTicket` renders master data with `InnerText` (which *does* encode) and
`InnerHtml` only for a fixed FontAwesome icon, so the creation page is clean.

---

## 6. Input Validation and Request Forgery

| Control | State |
|---|---|
| Server-side hierarchy validation | **Present** — `ValidateHierarchy` + `ValidateMasterDataIds` |
| Title required | **Present** — `RequiredFieldValidator` + `string.IsNullOrWhiteSpace` in BLL |
| Title length | `MaxLength=200` client-side; column `nvarchar(200)`; no server-side length check (truncation/exception depends on provider) |
| Description length | **Unbounded**, no server-side limit |
| Status ID integrity | **Present** — target status must exist and be active |
| Priority ID integrity | **Present** — must exist and be active |
| Assignee integrity | **Present** — must be an active employee |
| Comment length | `MaxLength=2000` client-side only; column `nvarchar(max)` |
| Resubmission token / duplicate submit | JS `_submitting` flag only — client-side, bypassable |
| **CSRF token** | **None.** No `ViewStateUserKey`, no anti-forgery token, no `Origin`/`Referer` check |
| HTTP verb enforcement | Web Forms postbacks are POST by default; no explicit verb check |

`SEC-09` (CSRF): Web Forms `ViewState` MAC plus `EventValidation` make blind
cross-site postbacks harder, but they are not an anti-CSRF mechanism for an
authenticated user, since a cross-site form post cannot forge a valid `__VIEWSTATE`
for a fresh page instance but *can* replay one if obtained. No test was run; the
finding is recorded from absence of any control, not from a demonstrated exploit.

---

## 7. Concurrency as a Security Control (correction of prior finding)

The prior Stage 1 audit claimed the `rowversion` check did not work (M-03). **This
review refutes that claim.** `LoadMutableTicket` loads the tracked entity and then
assigns the submitted token to `OriginalValue`, so EF6 adds
`WHERE [RowVersion] = @OriginalValue` to the `UPDATE`. A concurrent modification
produces `DbUpdateConcurrencyException`, which all six mutation methods catch,
rollback, and report.

Security-relevant consequence: two agents cannot silently overwrite each other's
status change, reassignment, or approval. That is a genuine integrity control and it
is correctly implemented.

---

## 8. Information Disclosure

| Vector | Behaviour | Assessment |
|---|---|---|
| Ticket ID enumeration | `?id=` parsed as `int`; non-authorized → "not found", same as missing | Good (no 403/404 distinction) |
| Queue cross-tenant metadata | title/requester/assignee visible | `SEC-03` |
| Error messages | `TicketList.BindQueue` catch → `"Error loading ticket queue: " + ex.Message` | **Leaks internal exception text to the UI** (`SEC-10`) |
| Create-ticket errors | `InvalidOperationException.Message` shown with a correlation ID; generic message for other exceptions | Mixed: business messages are intentional, unexpected ones are masked |
| Email body | contains title (HTML-encoded) and a relative link | No secret exposure; link is unusable (`WF-12`) |
| Audit trail | visible to anyone who can view the ticket | Intended |
| Internal comments | filtered out unless `CanAddInternalNote` | Good |
| `Web.config` | not read in this review | — |

---

## 9. File and Attachment Security

`itsm.TicketAttachments` exists with `FileName`, `FilePath`, `UploadedByCode`,
`UploadedOn`. **There is no upload control, no save path, no validation service, no
download handler, and no `FileUpload` anywhere in the ITSM module** (verified by
repository-wide search). The only `FileUpload`/`SaveAs` occurrences in the solution
belong to other modules (`Aata_Maida_Form`, HRMS import, Monthly Claims).

Consequences for this review:

- There is no attachment attack surface today — and therefore no content-type,
  magic-byte, size, path-traversal, or execution risk to assess.
- The governance documents (`compliance-and-security.md` §1) describe a 20 MB limit,
  an extension whitelist in `AttachmentValidationService`, storage outside the web
  root, and a `DownloadAttachment.ashx` authorization gate. None of these artefacts
  exist. The control description is **aspirational**, not AS-IS.
- `Web.config` upload limits (`maxRequestLength`) could not be verified.

---

## 10. Security Findings Register

| ID | Area | Finding | Classification |
|---|---|---|---|
| SEC-01 | Rendering | `Description`, `Title`, `ResolutionText`, CSAT feedback and lookup names rendered via `Label.Text` with no encoding → stored XSS | Architectural Gap |
| SEC-02 | Authorization | `CanProcessTicket` = requester OR assignee only; an agent with `ITSM.Ticket.View` cannot process; a requester can resolve/close/override on their own ticket | Architectural Gap |
| SEC-03 | Tenant | Queue queries have no `CompanyId` predicate; cross-company ticket title/requester/assignee visible in all four tabs | Configuration Required (policy) |
| SEC-04 | Authorization | `CreateTicket.aspx.cs` performs **no** `HasPermission` / `RequirePermission` call; page access rests on session presence only | Architectural Gap |
| SEC-05 | Tenant | `NULL CompanyId` blocks all mutations (fail-closed, deliberate, no remediation path) | Architectural Gap |
| SEC-06 | Permissions | Permission set cached in Session and refreshed only explicitly; grant/revoke is not reflected mid-session | Architectural Gap |
| SEC-07 | Identity | No `EmployeeStatus` check on page load or mutation entry for the actor | Clarification Required |
| SEC-08 | Session | `EnsureSessionValid` returns early without `LoginToken`; session invalidation suspended during DB faults by design | Clarification Required |
| SEC-09 | CSRF | No anti-forgery token, `ViewStateUserKey`, or Origin check on any ITSM mutation | Architectural Gap |
| SEC-10 | Disclosure | `TicketList` surfaces `ex.Message` to the browser | Architectural Gap |
| SEC-11 | Identity | Actor identity always server-derived from `Session["WORKMAN"]`; no browser-supplied actor/company accepted | Already Exists |
| SEC-12 | Authorization | Every mutation re-checks authorization inside the transaction against the loaded row | Already Exists |
| SEC-13 | Concurrency | `rowversion` optimistic concurrency works and prevents lost updates (refutes prior M-03) | Already Exists |
| SEC-14 | Attachments | No upload/download implementation exists, so no attachment security control is in force | Verified Absence |

---

## 11. Security Questions Requiring Owner Input

1. Is cross-company queue visibility accepted as residual risk, or is it a defect?
2. Should `CreateTicket.aspx` require an explicit ITSM permission rather than session
   presence?
3. Should a requester be able to resolve or close their own ticket?
4. Should status be re-checked against `EmployeeStatus` on each request, accepting the
   extra query per page load?
5. Is CSRF a concern for this intranet deployment (which would change `SEC-09`
   severity)?
6. Is attachment upload in scope for Phase 1B at all? If so, the governance documents
   describe controls that must not be cited as existing.
