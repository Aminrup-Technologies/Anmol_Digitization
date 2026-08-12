# ITSM Technical Implementation Guidelines

## Status

Accepted

## Context

These guidelines translate the data architecture ([Phase 1 Schema](../05-data-architecture/phase-1-schema.md)) and compliance controls ([Compliance & Security](../05-data-architecture/compliance-and-security.md)) into concrete implementation requirements for the ITSM Modular Monolith (`~/ITSM/`). They apply to all developers working on BLL services, EF6 data access, IIS configuration, and background processing.

---

## 1. SLA Polling (Background Processing)

SLA breach warnings must not rely on user activity. A **scheduled job** polls open tickets and enqueues notifications before deadlines are missed.

### 1.1 Scheduler requirement

Deploy one of the following (per environment standard):

| Option | Implementation |
| :--- | :--- |
| **SQL Server Agent Job** *(preferred)* | T-SQL or `EXEC` of a stored procedure called by `SlaPollingService` |
| **Windows Service** | Lightweight .NET 4.8 service referencing `~/ITSM/BLL/SlaPollingService.cs` |

**Frequency:** every **15 minutes**.

### 1.2 Polling logic

On each run, `SlaPollingService` queries `itsm.Tickets` joined to `itsm.MST_Priority` and `itsm.MST_TicketStatus` for tickets where:

- Status is **not terminal** (`MST_TicketStatus.IsTerminal = 0`)
- `ResolvedOn` IS NULL (ticket still active)
- SLA due date (computed by `SlaService` using `dbo.MST_Calendar` business-day math) has been reached or approached

For each qualifying ticket, evaluate two thresholds:

| Threshold | Condition | Action |
| :--- | :--- | :--- |
| **80% warning** | Elapsed SLA time ≥ 80% of `MST_Priority.SlaResolutionHours` | Insert one `dbo.NotificationQueue` row to assignee + supervisor |
| **100% breach** | Current time ≥ SLA due date | Insert one `dbo.NotificationQueue` row to assignee, supervisor, and IT management |

### 1.3 NotificationQueue insert

All SLA warnings use the existing shared infrastructure (see [Phase 1 Schema §2](../05-data-architecture/phase-1-schema.md#2-shared-infrastructure-write-append)):

```
SourceModule  = 'ITSM'
EventType     = 'SlaWarning80' | 'SlaBreach100'
RecipientEmail = <from MST_UserMaster>
Payload       = JSON with TicketNumber, threshold, SLA due date, deep link to ~/ITSM/UI/TicketDetail.aspx
```

**Idempotency:** track sent warnings in `itsm.TicketSlaAlerts` (or equivalent flag columns) so the same threshold is not re-enqueued on every 15-minute poll.

---

## 2. Agent Collision (Optimistic Concurrency)

Multiple IT agents may open the same ticket simultaneously. Without concurrency control, the last `SaveChanges()` silently overwrites the first agent's edits.

### 2.1 Schema requirement

`itsm.Tickets` includes a **`RowVersion`** column (`TIMESTAMP` / `rowversion` in SQL Server). See [Phase 1 Schema §4.1](../05-data-architecture/phase-1-schema.md#41-ismtickets).

### 2.2 EF6 mapping

```csharp
// ~/ITSM/DAL/Entities/Ticket.cs
[Table("Tickets", Schema = "itsm")]
public class Ticket
{
    // ... other properties ...

    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

EF6 maps `[Timestamp]` to SQL Server `rowversion`. The column is **database-generated** on every update; it must never be set manually in application code.

### 2.3 BLL enforcement

Every ticket update path in `~/ITSM/BLL/` must:

1. Load the ticket with its current `RowVersion`.
2. Apply field changes.
3. Call `SaveChanges()` inside a `try/catch` for `DbUpdateConcurrencyException`.

On `DbUpdateConcurrencyException`:

- **Do not** retry blindly with stale data.
- Return a user-facing message: *"This ticket was modified by another agent. Please refresh and re-apply your changes."*
- Log the collision via the application logger (see [Compliance §2](../05-data-architecture/compliance-and-security.md#2-application-logging--siem-integration-itgc-operations)).

The UI (`TicketDetail.aspx`) should display the conflict and reload the latest ticket state on refresh.

---

## 3. IIS File Upload Limits

ITSM attachments are limited to **20 MB** per file (see [Compliance §1](../05-data-architecture/compliance-and-security.md#1-attachment-security--malware-vectors-iso-27001) for extension whitelisting). IIS and ASP.NET must be configured to accept uploads up to this size.

### 3.1 Web.config requirements

Update `AnmolDristi/Web.config` with both limits — **both** are required; the lower of the two effective limits wins if only one is set.

**`system.web` — `httpRuntime`:**

```xml
<httpRuntime targetFramework="4.5" maxRequestLength="20480" executionTimeout="3600" />
```

`maxRequestLength` is in **kilobytes**. `20480` KB = 20 MB.

**`system.webServer` — `requestFiltering`:**

```xml
<security>
  <requestFiltering>
    <requestLimits maxAllowedContentLength="20971520" />
  </requestFiltering>
</security>
```

`maxAllowedContentLength` is in **bytes**. `20971520` = 20 MB.

### 3.2 BLL validation

`AttachmentValidationService` must reject files exceeding 20 MB **before** writing to disk, even if `Web.config` allows a larger limit. This provides defense in depth and a clear error message to the user.

---

## 4. DbContext Lifecycle

`ItsmDbContext` (EF6) holds database connections and change-tracked entities. Improper disposal under IIS causes **memory leaks** and connection-pool exhaustion when contexts are created per request but never released.

### 4.1 Coding standard (mandatory)

**Every** `ItsmDbContext` instantiation in `~/ITSM/BLL/` **must** be wrapped in a `using` block:

```csharp
// CORRECT
public void UpdateTicket(int ticketId, TicketUpdateDto dto)
{
    using (var db = new ItsmDbContext())
    {
        var ticket = db.Tickets.Find(ticketId);
        // ... apply changes ...
        db.SaveChanges();
    }
}
```

```csharp
// FORBIDDEN — context never disposed
public void UpdateTicket(int ticketId, TicketUpdateDto dto)
{
    var db = new ItsmDbContext();
    var ticket = db.Tickets.Find(ticketId);
    db.SaveChanges();
}
```

### 4.2 Additional rules

| Rule | Rationale |
| :--- | :--- |
| Do **not** store `ItsmDbContext` as a static or instance field on BLL service classes | Prevents long-lived contexts across requests |
| Do **not** pass `ItsmDbContext` from the UI code-behind into BLL methods | UI must call BLL service methods; BLL owns context lifetime |
| Prefer one `using` block per public BLL method | Clear ownership; avoids nested context sharing bugs |
| Long-running background jobs (`SlaPollingService`, `TicketClosureService`) use one `using` block per batch iteration | Disposes context after each 15-minute poll cycle |

Code reviews must reject any PR that introduces an undisposed `ItsmDbContext`.

---

## 5. Email Loop Prevention

MVP outbound notifications are **send-only** via `dbo.NotificationQueue`. Inbound email parsing (IMAP mailbox listener, auto-ticket creation from replies) is **deferred to Phase 2**.

### 5.1 MVP email requirements

Every ITSM email enqueued to `NotificationQueue` must include:

1. A strict **"Do not reply to this email"** disclaimer in the body footer.
2. A **hyperlink** to the ticket detail page, e.g.:
   ```
   https://<host>/ITSM/UI/TicketDetail.aspx?id={TicketId}
   ```
3. `Reply-To` header set to a no-reply address (e.g. `noreply@anmolindustries.com`) or omitted entirely.

**Example footer (HTML):**

```html
<hr />
<p style="color:#666;font-size:12px;">
  This is an automated message from the ITSM system. <strong>Do not reply to this email.</strong>
  To view or update this ticket, click
  <a href="https://{host}/ITSM/UI/TicketDetail.aspx?id={TicketId}">here</a>.
</p>
```

### 5.2 Phase 2 deferral

The following are **out of scope for MVP**:

- IMAP/POP3 mailbox polling for inbound replies
- Parsing email threads into `TicketComments`
- Creating new tickets from forwarded emails

Document any inbound-email requirements in a future ADR before implementation.

---

## Related Documents

- [Phase 1: Data Architecture & Schema Design](../05-data-architecture/phase-1-schema.md)
- [ITSM Compliance & Security Controls](../05-data-architecture/compliance-and-security.md)
- [ADR 0001 — ITSM Application Architecture Strategy](../04-adrs/0001-itsm-architecture-strategy.md)
