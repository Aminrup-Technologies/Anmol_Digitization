# ITSM Requirements Gap Analysis

**Document:** 5 of 6 — Requirements vs AS-IS
**Purpose:** Determine, from evidence, whether the existing architecture already supports
each capability raised in tester feedback. Not a TO-BE design.
**Mode:** Read-only.
**Date:** 2026-11-12

---

## 1. Executive Summary

Eleven capabilities were investigated. The result is deliberately conservative: a
capability is only marked **Already Exists** when an end-to-end, reachable
implementation was read.

| Classification | Count | Items |
|---|---|---|
| **Already Exists** | 1 | Item/Title validation |
| **Partially Exists** | 4 | IT root category & hierarchy, impact/urgency indicators, category-based assignment, agent hierarchy independence |
| **Missing** | 6 | Attachments, confirmation popup, SAP/Non-SAP grouping, resolution impact, resolution by authorized agent, direct-to-Resolved |
| **Configuration Required** | 0 | — |
| **Architectural Gap** | 0 standalone | (gaps are recorded per item below and consolidated in Document 6) |

**The critical distinction for planning:** six items are not "absent because nobody
looked in the right file". Each was traced through UI, BLL, DAL, and schema. Where
something is missing, the *supporting schema or hook exists* (attachments table,
resolution text, transition map) but the behaviour does not.

**Nothing in this analysis authorises implementation.** Classifications are
descriptive only.

---

## 2. Capability-by-Capability Findings

### GAP-01 — IT root category with hierarchical category selection

**Classification: Partially Exists**

| Dimension | Evidence |
|---|---|
| Schema | `itsm.MST_Category` → `MST_SubCategory` → `MST_Item` (three FK-linked tables, `202608120550515_InitialItsmSchema.cs`) |
| API | `TicketMasterDataService.GetCategoryHierarchy(int companyId)` returns nested `TicketCategoryDto → TicketSubCategoryDto → TicketItemDto` |
| UI | Step 2 "Root Category" clickable cards, Step 3 Sub Category, Step 4 Item — all populated from a JSON blob in `hfCategoryData`, filtered client-side with no postbacks |
| Server validation | `ValidateHierarchy(categoryId, subCategoryId, itemId)` — subcategory must belong to category and be active; item must belong to subcategory and be active |
| Persistence | `Tickets.CategoryId`, `SubCategoryId`, `ItemId` (item nullable) |

**What exists:** a working three-tier hierarchical selection with server-side
re-validation. This is real, not cosmetic.

**What does not exist:** an "IT" root node. `MST_Category` has no `ParentId`, `Level`,
`IsRoot`, or `CompanyId` column (confirmed in the migration). The taxonomy is exactly
three fixed tiers and the top tier is a flat list of categories (Hardware, Software,
Network per the design doc). A single parent "IT" node under which all categories sit
is **not representable**, and categories are **global** — `GetCategoryHierarchy`
accepts `companyId` and never uses it (`ARC-05`).

**Implication:** the hierarchy requirement is met in shape, not in the specific
"IT root" form. Whether an explicit IT root is required (for display, reporting, or
routing) cannot be answered from code; it is a requirements question, not an
implementation question.

**Unverified:** actual category seed content.

---

### GAP-02 — Item or Title validation

**Classification: Already Exists**

| Rule | Implementation | Verified |
|---|---|---|
| Title required (client) | `<asp:RequiredFieldValidator ControlToValidate="txtTitle" ValidationGroup="CreateTicket">` | Yes |
| Title required (server) | `TicketService.CreateTicket`: `if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.")` | Yes |
| Title length cap | `MaxLength="200"` on the TextBox; column `nvarchar(200) NOT NULL` | Yes |
| Title trimmed | `Title = title.Trim()` | Yes |
| Item optional | `ItemId` is `int?` end-to-end; UI labels it "optional" | Yes |
| Item validated when supplied | `ValidateHierarchy` → item must exist, be `IsActive`, and belong to the selected subcategory | Yes |
| Category/subcategory integrity | subcategory must belong to category and be active | Yes |
| Other master IDs | `ValidateMasterDataIds` requires impact, urgency, request type all exist and active | Yes |
| Client pre-check | `onSubmit()` refuses empty requestType/category/subcategory/impact/urgency, then runs `Page_ClientValidate('CreateTicket')` | Yes |

**Observation:** "Item **or** Title" is not implemented as an alternation. Title is
always mandatory; Item is always optional and independent of Title. If the
requirement means "require Item when Title is insufficient" or "require one of the
two", that specific rule does not exist. If it means "validate both when present",
it is satisfied.

**Classification caveat:** recorded as Already Exists on the straightforward reading;
the alternation reading is `Clarification Required` (see §4, UQ-2).

---

### GAP-03 — Ticket attachment uploads

**Classification: Missing**

| Layer | State |
|---|---|
| Schema | `itsm.TicketAttachments` complete: `Id`, `TicketId` (FK), `FileName nvarchar(255)`, `FilePath nvarchar(500)`, `UploadedByCode` (FK), `UploadedOn` |
| Entity | `TicketAttachment.cs` correct, with `Ticket` and `UploadedBy` navigations; `ItsmDbContext.TicketAttachments` DbSet registered |
| Navigation | `Ticket.Attachments` collection exists |
| UI (create) | **No `FileUpload` control** in `CreateTicket.aspx` |
| UI (detail) | **No attachment list, no download link** in `TicketDetail.aspx` |
| BLL | **No upload method**, no `TicketAttachment` write anywhere |
| Download handler | `DownloadAttachment.ashx` **does not exist** |
| Validation service | `AttachmentValidationService` **does not exist** |

Verified by repository-wide search: the only `FileUpload`/`SaveAs` occurrences belong
to `Aata_Maida_Form`, HRMS import, and Monthly Claims — none to ITSM.

**Implication:** the data model anticipates attachments; the feature is entirely
unimplemented. Governance documentation describing a 20 MB limit, an extension
whitelist, off-web-root storage, and an authorized download handler describes a
**target** state. Those controls must not be cited as current in any assessment.

---

### GAP-04 — Submission confirmation popup

**Classification: Missing**

| Expected | Observed |
|---|---|
| Modal/dialog confirming submission, ideally with ticket number | None |
| Success message on the creation page | `ShowMessage(message, success)` exists, but **every call site in `CreateTicket.aspx.cs` passes `success: false`** |
| Post-submit navigation | `Response.Redirect("~/ITSM/UI/TicketDetail.aspx?id=" + newTicketId, false)` + `CompleteRequest()` — Post/Redirect/Get |
| Client busy state | `#submitSpinner` text "Creating ticket..." plus a JS `_submitting` flag |

The user's confirmation experience today is: the form is replaced by the ticket
detail page. The ticket number is visible there, so the *information* is delivered —
but there is no confirmation popup, and no success banner anywhere in the flow.

**Implication:** the requirement is unmet as specified. The existing PRG redirect is
the current confirmation mechanism and is a defensible one; the gap is presentational
and product-facing.

---

### GAP-05 — Impact and urgency visual indicators

**Classification: Partially Exists**

| Surface | Implementation | Visual indication |
|---|---|---|
| Create form | `BindImpacts`/`BindUrgencies` render server-side `span` button groups; JS `applySemanticGroup` derives level from label text and applies `itsm-ct__btn--low/medium/high` + FontAwesome icon | **Yes** — green/amber/red borders, fills, icons (`fa-user`/`fa-users`/`fa-building`, `fa-clock-o`/`fa-line-chart`/`fa-hand-paper-o`), checkmark badge on selection |
| Ticket list | Priority badge only (`badge-p1..p4`, red/orange/yellow/green) | Impact/urgency **not shown** |
| Ticket detail | `lblImpactUrgency.Text = dto.ImpactName + " / " + dto.UrgencyName` | **Plain text**, no colour, no icon, no severity styling. Priority is a separate badge. |

**Observation:** severity semantics are inferred **client-side from the display
name** (`semanticLevel` looks for the substrings "high", "medium", "low", falling back
to list index). If a lookup is renamed (e.g. "Severe"), the visual mapping degrades to
positional defaults. The database has `SortOrder` on impact/urgency but the JS does
not receive it — `TicketLookupItemDto` carries only `Id` and `Name`.

**Implication:** the creation-time indicator requirement is largely satisfied; the
in-ticket indication is not. Consistency across the three surfaces does not exist.

---

### GAP-06 — SAP and Non-SAP agent grouping across PAN India

**Classification: Missing**

| Probe | Result |
|---|---|
| Any `SAP`/`NonSAP` column in `itsm` tables | None in any of the 15 tables |
| Any SAP field in ITSM entities | None |
| Any `SAP` token in `AnmolDristi/ITSM/**` or ITSM SQL | **Zero matches** |
| Agent grouping construct (group/team/pool/queue entity) | None |
| Geographic scope construct | `MST_SupportMapping.BranchId NOT NULL` — every mapping is one branch |
| Agent identity | Single `SupportAgentCode` per mapping row |

The legacy `dbo.MST_Plant` is documented as carrying `SAP_Code`, but ITSM does not
read it and does not map it to any entity consumed by dispatch.

**Implication:** neither the SAP/Non-SAP split nor PAN-India (multi-branch or global)
agent grouping has any representation. This is the largest structural distance between
the reviewer's expectation and the implementation: the model assumes
**one branch + one taxonomy → one named employee**.

---

### GAP-07 — Category-based automatic assignment matrix

**Classification: Partially Exists**

| Element | State |
|---|---|
| Matrix table | `itsm.MST_SupportMapping` |
| Dimensions | `BranchId` (required), `CategoryId?`, `SubCategoryId?`, `ItemId?`, `IsBranchDefault` |
| Resolution | `DispatchService.ResolveAssignee` — 4-step cascade, most specific wins, `NULL` if no match |
| Invocation | `TicketService.CreateTicket` step 9, before insert; result stored in `Tickets.AssigneeCode` |
| Category-based | **Yes** — category is step 3 of the cascade |
| Automatic | **Yes** — no human action; assignee set before first `SaveChanges()` |
| Determinism | **Weak** — duplicates possible, `FirstOrDefault` with no `OrderBy` |
| Configuration tooling | **None** — no ITSM admin page manages `MST_SupportMapping` |
| Request-type dimension | **Absent** — mapping has no `RequestTypeId` |

**What qualifies it as Partial rather than Exists:** (a) `BranchId` is mandatory, so a
category-only, nation-wide rule cannot be expressed; (b) ties are non-deterministic;
(c) there is no UI or service to maintain the matrix — rows must be inserted by DBA
script; (d) suppression is silent — no match yields `NULL` with no supervisor-queue
flag or notification, despite the design doc claiming a "supervisor queue".

**Unverified:** whether any `MST_SupportMapping` rows exist in any environment.

---

### GAP-08 — Agent hierarchy modification without changing Request Type

**Classification: Partially Exists**

Two distinct questions are embedded here.

**(a) Is assignment coupled to Request Type?**
**No — decoupled by construction, verified.** `MST_SupportMapping` has no
`RequestTypeId`; `MST_RequestType` holds only `Code`, `Name`, `Prefix`, the two
approval flags, `IsActive`. Changing an assignment therefore cannot require changing
a Request Type, and vice versa.

**(b) Is there an agent hierarchy that can be modified?**
**No.** There is no agent entity. The only agent references are
`MST_SupportMapping.SupportAgentCode` and `Tickets.AssigneeCode` — both point at
`dbo.MST_UserMaster.EmployeeCode`. There is no reporting line for agents, no group,
no level, no supervisor, no skill, no capacity. `ApprovalConfiguration` names one
global IT Head — an approval role, not an assignment hierarchy.

**Implication:** the *independence* the reviewer wants already holds; the *hierarchy*
to be modified does not exist. Assignment changes today are direct data edits to one
table, with no versioning, no audit, and no UI.

---

### GAP-09 — Resolution impact capture

**Classification: Missing**

| Probe | Result |
|---|---|
| Resolution fields on `Tickets` | `ResolutionText nvarchar(max)`, `ResolvedOn` — **no impact field** |
| Resolution fields on `TicketAuditLog` | `Detail nvarchar(1000)` (unused for status changes) |
| Resolution UI | `divResolution` shows a single multiline `txtResolution` when the selected target status text is "Resolved" |
| Resolution DTO | `TicketDetailDto.ResolutionText` only |
| Business impact capture at closure | None |

"Resolution impact" — the outcome/effect of the fix, distinct from the narrative of
what was done — has no column, no DTO field, no control, and no validation. The only
free text is `ResolutionText`, whose audit copy is not even retained (`WF-10`).

---

### GAP-10 — Resolution by both assignee and authorized agent

**Classification: Missing**

```csharp
public static bool CanProcessTicket(string actorCode, string requesterCode, string assigneeCode)
{
    return IsRequesterOrAssignee(actorCode, requesterCode, assigneeCode);
}
```

`CanProcessTicket` takes **no permission parameter**. All four work actions
(`ChangeStatus`, `ReassignTicket`, `OverridePriority`, `AddComment`) gate on it.

So the resolution-capable set is exactly `{requester, assignee}`. An agent who holds
`ITSM.Ticket.View` and can see the ticket (via the tenant-scoped permission path)
gets `pnlWorkActions` hidden — `pnlWorkActions.Visible = !dto.IsTerminal &&
dto.StatusName != "Pending Approval"` is computed **without** any permission test — and
even if the control were forced, `LoadForProcess` would reject the call.

The inverse also holds: a **requester** can resolve their own ticket.

**Implication:** "resolution by authorized agent" is not a supported role. The
architecture distinguishes *view* (relationship or permission+tenant) from *process*
(relationship only) and has no third tier for permission-based processing. This is a
coherent design, but it is not what the requirement describes.

---

### GAP-11 — Direct transition to Resolved without mandatory In Progress

**Classification: Missing**

`TicketWorkflow.Transitions`:

| From | Allowed |
|---|---|
| `New` | `In Progress`, `On Hold`, `Cancelled` |
| `In Progress` | `Resolved`, `On Hold`, `Cancelled` |
| `On Hold` | `In Progress`, `Cancelled` |
| `Resolved` | `Closed`, `In Progress` |

- `New → Resolved`: **not permitted** — `IsValidTransition("New","Resolved")` returns `false`.
- `On Hold → Resolved`: **not permitted**.
- The only route to `Resolved` is from `In Progress` (or `Resolved` itself, a no-op).

`In Progress` is therefore **mandatory** by construction. Enforced in two places: the
map (`GetAvailableStatuses` populates the dropdown) and `ChangeStatus`
(`IsValidTransition` before any write). A crafted postback cannot bypass it.

**Additional constraint that applies even if the map were extended:** the
`IsTerminal` check runs *before* the transition check (`WF-04`), and a status-name
comparison (`targetStatus.Name == "Resolved"`) drives resolution-text enforcement, so
any change here is coupled to seed naming.

---

## 3. Cross-Cutting Observations

These are not reviewer items, but they condition how the eleven items can be judged.

| # | Observation | Relevance |
|---|---|---|
| X1 | **No ITSM stored procedures, views, or triggers exist.** All behaviour is C#. | Every workflow guarantee is application-enforced; no DB safety net |
| X2 | **`MST_TicketStatus.IsTerminal` seed values are unknown and decisive.** They determine whether `Resolved → Closed` is reachable at all. | Undermines any confident statement about closure/CSAT |
| X3 | **`CsatScore`/`CsatFeedback` have no writer.** | Any "closure" story is display-only |
| X4 | **SLA start/due are computed and discarded.** | Impact/urgency → priority → SLA chain terminates without persistence |
| X5 | **Assignment configuration has no maintenance UI.** | GAP-07's "matrix" is a table, not a managed capability |
| X6 | **Request Type carries only approval flags.** | It is not a routing or taxonomy dimension |
| X7 | **Categories are global; no tenant column.** | GAP-01's "IT root" has no tenancy story either |

---

## 4. Unresolved Questions Raised by This Analysis

| # | Question | Why it blocks a verdict |
|---|---|---|
| UQ-1 | Is an explicit "IT" root node required, or is "root category" simply the top tier? | Changes GAP-01 from Partial to Exists/Missing |
| UQ-2 | Does "Item or Title validation" mean an alternation rule, or validate-both-when-present? | Changes GAP-02 |
| UQ-3 | Is the confirmation "popup" a hard requirement, or is redirect-to-detail acceptable? | Changes GAP-04 from Missing to Exists |
| UQ-4 | Are impact/urgency indicators required on the detail page too, or only at creation? | Changes GAP-05 scope |
| UQ-5 | What does "agent hierarchy" mean operationally — reporting line, skill group, or SAP/Non-SAP split? | GAP-06 and GAP-08 cannot be scoped without it |
| UQ-6 | Must the assignment matrix be maintainable by an administrator, or is DBA-maintained data acceptable? | Changes GAP-07 |
| UQ-7 | Is "resolution impact" a structured value (code/severity) or free text? | Determines whether `ResolutionText` could ever satisfy it |
| UQ-8 | Who is an "authorized agent" — any agent holding `ITSM.Ticket.View`, a role, or an explicit assignment? | Changes GAP-10 and `SEC-02` |
| UQ-9 | Is `Resolved` intended to be terminal? | Changes GAP-11's blast radius entirely (`WF-04`) |
| UQ-10 | Are attachments and CSAT in Phase 1B scope? | Both are documented as existing but are not implemented |

---

## 5. Evidence Index for This Analysis

| Capability | Primary files read |
|---|---|
| GAP-01 | `MST_Category.cs`, `MST_SubCategory.cs`, `MST_Item.cs`, `TicketMasterDataService.cs`, `CreateTicket.aspx`, migration 1 |
| GAP-02 | `CreateTicket.aspx`, `CreateTicket.aspx.cs`, `TicketService.cs`, `TicketMasterDataService.cs` |
| GAP-03 | `TicketAttachment.cs`, `ItsmDbContext.cs`, `Ticket.cs`, `CreateTicket.aspx`, `TicketDetail.aspx`, migration 1, repo-wide search |
| GAP-04 | `CreateTicket.aspx`, `CreateTicket.aspx.cs` |
| GAP-05 | `CreateTicket.aspx` (CSS + JS), `TicketDetail.aspx(.cs)`, `TicketList.aspx(.cs)` |
| GAP-06 | all 21 entities, migration 1, `DispatchService.cs`, repo-wide SAP search |
| GAP-07 | `DispatchService.cs`, `MST_SupportMapping.cs`, `TicketService.cs`, migration 1 |
| GAP-08 | `MST_RequestType.cs`, `MST_SupportMapping.cs`, `ApprovalConfiguration.cs`, `ApprovalConfigurationService.cs` |
| GAP-09 | `Ticket.cs`, `TicketDetailService.cs`, `TicketUpdateService.cs`, `TicketDetail.aspx` |
| GAP-10 | `ItsmTicketAccess.cs`, `TicketUpdateService.cs`, `TicketDetailService.cs`, `TicketDetail.aspx.cs` |
| GAP-11 | `TicketWorkflow.cs`, `TicketUpdateService.ChangeStatus`, `TicketDetailService` |

No file outside `AnmolDristi/ITSM/`, `AnmolDristi/Database/`, the ITSM migrations, and
`docs/05-data-architecture/` was used to reach these classifications.
