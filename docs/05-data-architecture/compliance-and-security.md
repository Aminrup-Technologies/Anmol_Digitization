# ITSM Compliance & Security Controls

## Status

Accepted

## Context

The ITSM module in `AnmolDristi` handles sensitive operational data — ticket descriptions, attachments, approval chains, and customer satisfaction feedback. As part of the Modular Monolith described in [ADR 0001](../04-adrs/0001-itsm-architecture-strategy.md) and the data model in [Phase 1 Schema](./phase-1-schema.md), the following controls satisfy **IT General Controls (ITGC)** and **ISO 27001** requirements for logical access, operations, and data lifecycle management.

All enforcement points live in `~/ITSM/BLL/` unless noted otherwise. The UI layer must not bypass these rules.

---

## 1. Attachment Security & Malware Vectors (ISO 27001)

Ticket attachments are stored on the IIS file system via `itsm.TicketAttachments` (see [Phase 1 Schema §4.4](./phase-1-schema.md#44-itsmticketattachments)). Because uploaded files are a primary malware vector, the ITSM module enforces a **strict extension whitelist** at the BLL layer before any file is written to disk.

### 1.1 Allowed file extensions

`AttachmentValidationService` in `~/ITSM/BLL/` rejects any upload whose extension is not on the whitelist. Validation is case-insensitive and applies to both the client-provided filename and the detected content type where available.

| Category | Allowed extensions |
| :--- | :--- |
| Documents | `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.txt`, `.csv` |
| Images | `.png`, `.jpg`, `.jpeg`, `.gif` |
| Archives | `.zip` *(optional — enable only if business requires; scanned server-side before extraction)* |

**Blocked by default:** executable and script extensions including but not limited to `.exe`, `.bat`, `.cmd`, `.ps1`, `.vbs`, `.js`, `.aspx`, `.asp`, `.ashx`, `.asmx`, `.dll`, `.msi`, `.scr`, `.hta`, `.html`, `.htm`, `.svg`.

If validation fails, the BLL throws a user-facing exception and **no file is persisted** — no `TicketAttachments` row and no disk write.

### 1.2 IIS upload directory hardening

Attachments are stored outside the web application's executable path (e.g. `D:\ITSM_Uploads\` or a dedicated virtual directory **without** script execution):

- The upload folder must be configured in IIS with **Execute permissions disabled** (static content only).
- **Request Filtering** must block script-handler mappings for the upload path (no `.aspx`, `.asp`, `.ashx`, or other ISAPI/CGI handlers).
- Files are served via a controlled download handler in `~/ITSM/UI/DownloadAttachment.ashx` (or equivalent) that validates the requesting user's authorization against the parent ticket — never via direct URL to the raw file path.
- Antivirus scanning of inbound uploads is recommended at the infrastructure layer (Windows Defender, corporate AV agent, or ICAP proxy) in addition to extension whitelisting.

---

## 2. Application Logging & SIEM Integration (ITGC Operations)

ITGC requires demonstrable evidence of system operation, error handling, and access monitoring. The ITSM module must implement an **application-level logger** (NLog or Serilog, consistent with team standards) that captures structured events from the BLL and UI exception handlers.

### 2.1 Events to log

| Event category | Examples | Minimum fields |
| :--- | :--- | :--- |
| **BLL exceptions** | Unhandled errors in `DispatchService`, `ApprovalWorkflowService`, `TicketClosureService` | Timestamp, `EmployeeCode`, ticket ID, exception type, stack trace (internal logs only), correlation ID |
| **Unauthorized access** | User attempts to view/approve a ticket outside their scope; failed `BasePage` session check on ITSM pages | Timestamp, `EmployeeCode`, requested resource, client IP, action attempted |
| **System errors** | Database connectivity failures, `SaveChanges()` violations, file I/O errors on upload | Timestamp, severity, component name, error message, correlation ID |

### 2.2 Log destinations

Logs are written to **one or both** of the following destinations (configured per environment):

1. **`itsm.ErrorLogs` table** (preferred for queryability and SIEM ingestion):

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `BIGINT` PK | Identity |
| `LoggedOn` | `DATETIME` | UTC |
| `Severity` | `NVARCHAR(20)` | `Info`, `Warning`, `Error`, `Fatal` |
| `Category` | `NVARCHAR(50)` | e.g. `UnauthorizedAccess`, `BLLException`, `SystemError` |
| `EmployeeCode` | `NVARCHAR(50)` NULL | Actor, if known |
| `TicketId` | `INT` NULL | Related ticket, if applicable |
| `Message` | `NVARCHAR(MAX)` | Human-readable summary |
| `ExceptionDetail` | `NVARCHAR(MAX)` NULL | Stack trace / inner exception (omit from user-facing views) |
| `ClientIp` | `NVARCHAR(45)` NULL | IPv4 or IPv6 |
| `CorrelationId` | `UNIQUEIDENTIFIER` NULL | Trace across layered calls |

2. **Windows Event Viewer** — fallback or parallel sink for environments where the SIEM agent reads the Application log. Use a dedicated event source (e.g. `AnmolDristi.ITSM`).

### 2.3 SIEM integration

- Log entries must be forwardable to the corporate SIEM (e.g. Splunk, QRadar, Sentinel) via the existing log-shipping agent or SQL CDC on `itsm.ErrorLogs`.
- **Retention:** operational error logs are retained for a minimum of **90 days** online; archival to cold storage follows corporate ITGC policy.
- Sensitive data (passwords, tokens, full attachment paths) must **never** appear in log messages.

---

## 3. Separation of Duties (SoD) for Change Requests (ITGC Logical Access)

ITGC logical-access controls require that no individual can both initiate and approve the same change. For **Change Requests (`CR`)**, the BLL enforces Separation of Duties in `ApprovalWorkflowService`.

### 3.1 SoD rule

When a `TicketApprovals` row is created or when an approver attempts to action an approval, the BLL **must throw an exception** (and log an `UnauthorizedAccess` event per Section 2) if:

```
Tickets.RequesterCode == TicketApprovals.ApproverCode
```

This applies to **all approval levels** on a CR — Manager and IT Head — and is evaluated independently at each level.

### 3.2 Implementation requirements

- The rule is enforced in `~/ITSM/BLL/ApprovalWorkflowService.cs` **before** `ApprovalStatus` is set to `Approved`.
- The UI (`~/ITSM/UI/ApproveTicket.aspx`) must not offer the requester as a selectable approver; the BLL check is the authoritative control (defense in depth).
- If the requester's `ReportingManagerCode` resolves to the requester themselves (data anomaly), the ticket is created in **Pending Approval** but the approval action is blocked until an IT supervisor reassigns the approver via an internal workflow exception process (logged in `TicketComments`, `IsInternal = 1`).
- **SR tickets** follow the same SoD rule where `RequesterCode` would equal the Manager `ApproverCode`.

---

## 4. Data Retention & Archiving (ISO 27001 & Privacy)

Closed tickets contain personal and operational data subject to privacy regulations and corporate retention schedules. The ITSM module defines a clear lifecycle for ticket data after closure.

### 4.1 Online retention period

| Data class | Retention (online) | Rationale |
| :--- | :--- | :--- |
| Closed tickets (`itsm.Tickets` where status = **Closed**) | **7 years** from `ClosedOn` | Aligns with typical financial/audit record-keeping requirements |
| Ticket comments, attachments, and approvals | Same as parent ticket | Cascading retention with the ticket record |
| Error logs (`itsm.ErrorLogs`) | **90 days** online (see §2.3) | Operational — not business records |

During the online retention window, closed tickets remain fully queryable for audit, reporting, and legal hold purposes.

### 4.2 Archival and purge process

A **scheduled SQL Server Agent job** (`ITSM_DataRetention_Archive`) runs monthly (or per corporate policy) and performs the following for tickets where `ClosedOn < DATEADD(YEAR, -7, GETDATE())`:

1. **Archive** — copy ticket header, comments, approvals, and attachment metadata to an archive schema or database (e.g. `itsm_archive.Tickets`, `itsm_archive.TicketComments`). Attachment files on disk are moved to an archive storage tier.
2. **Purge** — after successful archive verification, delete the corresponding rows from the active `itsm` schema and remove archived files from the hot upload directory.
3. **Audit** — insert a summary row into `itsm.DataRetentionLog` recording the job run date, record counts archived/purged, and any failures.

Tickets under **legal hold** (flagged by `Tickets.IsLegalHold = 1` or equivalent) are **excluded** from archival/purge until the hold is released.

### 4.3 Privacy compliance

- Archived data retains the same access controls as active data — only authorized ITSM roles and audit personnel may query archive tables.
- Personal data fields (`CsatFeedback`, requester contact details joined from `MST_UserMaster`) are included in archive scope and subject to the same retention limits.
- Right-to-erasure requests are handled through a manual exception process coordinated with Legal/Compliance; the scheduled job does not override legal-hold or active litigation flags.

---

## Related Documents

- [Phase 1: Data Architecture & Schema Design](./phase-1-schema.md)
- [ADR 0001 — ITSM Application Architecture Strategy](../04-adrs/0001-itsm-architecture-strategy.md)
- [ADR 0003 — ITSM Data Store Strategy](../04-adrs/0003-itsm-data-store-strategy.md)
