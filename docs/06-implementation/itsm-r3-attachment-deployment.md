# ITSM R3 attachment deployment prerequisite

R3 stores ticket files under the application path `~/UploadedFiles/ITSM/{ticketId}/{guid}{extension}` and records that relative path in the existing `itsm.TicketAttachments` table. Downloads in the application go through `ITSM/UI/DownloadAttachment.ashx`, which applies the existing ticket view rules.

## Direct folder access restriction included in the candidate

The candidate adds `UploadedFiles/ITSM/web.config`, scoped to the attachment storage folder, to deny HTTP handler access and IIS URL authorization. The application-pool identity retains filesystem access; the application download handler reads the file after its ticket/company authorization check. This configuration has not yet been verified on the UAT IIS site.

Before R3 UAT, publish the scoped folder configuration and confirm the IIS URL Authorization feature is installed and active. If the host does not honor this configuration, an authorized IIS-level deny rule must be applied to `~/UploadedFiles/ITSM/`:

- Script execution stays off for that folder.
- A direct GET of a file in that folder is denied.
- The application identity can still read the folder, because `DownloadAttachment.ashx` transmits the file from disk after the ticket view check.

Do not treat code review or a local build as evidence that the direct URL is blocked. Verify the denial on the UAT site after the IIS change is applied.

## Request size

The per-file limit enforced by the application is 20 MiB (20,971,520 bytes). This release does not change the site-wide request limit. A post whose total size exceeds that existing limit is rejected by IIS or ASP.NET before Create Ticket runs, so the ticket is not created and per-file failure messages are not shown.

ZIP remains disabled. No maximum file count is defined.
