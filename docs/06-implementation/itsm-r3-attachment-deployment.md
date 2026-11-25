# ITSM R3 attachment deployment prerequisite

R3 stores ticket files under the application path `~/UploadedFiles/ITSM/{ticketId}/{guid}{extension}` and records that relative path in the existing `itsm.TicketAttachments` table. Downloads in the application go through `ITSM/UI/DownloadAttachment.ashx`, which applies the existing ticket view rules.

## Direct folder access is not blocked by this release

This change does not deny anonymous or authenticated HTTP access to `/UploadedFiles/ITSM/`. A request that can guess or reuse a file URL may still be served by IIS as a static file. That was not verified in UAT, and this document does not claim that it is blocked.

Before R3 UAT, an authorized IIS or configuration change must deny direct access to `~/UploadedFiles/ITSM/`:

- Script execution stays off for that folder.
- A direct GET of a file in that folder is denied.
- The application identity can still read the folder, because `DownloadAttachment.ashx` transmits the file from disk after the ticket view check.

Do not treat code review or a local build as evidence that the direct URL is blocked. Verify the denial on the UAT site after the IIS change is applied.

## Request size

The per-file limit enforced by the application is 20 MiB (20,971,520 bytes). This release does not change the site-wide request limit. A post whose total size exceeds that existing limit is rejected by IIS or ASP.NET before Create Ticket runs, so the ticket is not created and per-file failure messages are not shown.

ZIP remains disabled. No maximum file count is defined.
