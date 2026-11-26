---
cursor:
  subagentId: "bc-b1e9a31d-0ec8-5cfb-811f-92fd0ee44480"
---

# ITSM R1–R5 UAT test results

Readiness: **FAIL**.

## Candidate

- Branch `cr/itsm-r1-r5-uat`
- Base `Aug26_AILWorks_AmimnrupSupport`
- Draft PR https://github.com/Aminrup-Technologies/Anmol_Digitization/pull/249 was left open, draft, and unmerged
- Code and IIS checks used `4fff90ff9a5a05e1a99e7a3a91ac62fbd4575d1d`
- This results file is the next commit on that branch

Remediation commits on the branch:

- `cbdadd0` deny direct ITSM attachment HTTP
- `21a7b08` include the folder web.config in the csproj
- `0af02ff` record the scoped attachment HTTP denial
- `4fff90f` change the Ticket Detail R5 note from an HTML comment to a server comment

`AnmolDristi/ITSM/UI/TicketDetail.aspx` at `4fff90f` starts with `<%--` and ends that note with `--%>`. The diff is those two marker lines only. The Create Ticket timing trace, the root `Web.config`, upload files, and build output were not staged.

## Build

MSBuild from Visual Studio 18 Build Tools, `/p:VSToolsPath=C:\Program Files (x86)\MSBuild\Microsoft\VisualStudio\v14.0`. IIS Express was not recycled.

| Build | Exit | Errors | Warnings |
| --- | --- | --- | --- |
| Debug | 0 | 0 | 33 |
| Release | 0 | 0 | 29 |

Debug warnings: 4 existing `MSB3305` for `zkemkeeper`, plus 29 existing compiler warnings (`CS0168` 19, `CS0219` 7, `CS0414` 1, `CS1998` 2). Release did not reprint the four COM warnings. No warning line in either log is under `ITSM`. No new warning was observed. The earlier Release count of 33 included those four COM warnings.

## Automated tests

`ItsmAttachmentTests.exe` exited 0 and printed `All ITSM attachment tests passed.` Those view checks call `AttachmentAccessRules.AllowsTicketView`. They are not the `CanViewTicket` results below.

## Ticket Detail

The parser correction is committed at `4fff90f` and was pushed. A signed-in open of Ticket Detail was not run.

## Direct IIS access

Site physical path is `D:\PersonalRnD\AminrupWorks\Anmol_Digitization\AnmolDristi` on `localhost:53713`.

`UploadedFiles/ITSM/web.config` is a csproj `Content` include. It sets `system.web` authorization deny for `*`, `handlers accessPolicy="None"`, and a `system.webServer` authorization deny for `*`. The comment says this is HTTP-only and does not remove application-pool file I/O. `DownloadAttachment.ashx` remains the download path in code.

Observed GET of `UploadedFiles/ITSM/51/865fc1b20a0040f192e6bcba8cd243fa.png`:

- HTTP 500.19, error `0x800700b7`
- Config error: cannot add duplicate collection entry of type `add` with users `*`, empty roles, empty verbs
- Config file: `AnmolDristi\UploadedFiles\ITSM\web.config`, the `system.webServer` authorization add
- Body was an HTML error page, 5568 bytes, not a PNG
- The same status and an HTML body, with no PNG signature, were returned for the ticket 52 file and for a missing file name
- After the rebuild, the ticket 51 URL again returned HTTP 500, `text/html`, and no PNG signature

The file bytes were not returned. The committed deny rule did not load. This is not an authorization denial.

## Live CanViewTicket

Called `ItsmTicketAccess.CanViewTicket` on `AnmolDristi.dll` with a null context. Company checks and requester/assignee checks return before any database call. Later checks do not.

| Case | Actual |
| --- | --- |
| Actor company 0 | false |
| Ticket company missing, requester matches | false |
| Cross-company requester | false |
| Cross-company assignee | false |
| Cross-company caller with `ITSM.Ticket.View` | false |
| Cross-company unassigned other actor | false |
| Same-company requester | true |
| Same-company requester, different case | true |
| Same-company assignee | true |
| Same-company assignee, different case | true |
| Same-company pending approver | NOT RUN |
| Same-company permission viewer | NOT RUN |
| Same-company mapped agent | NOT RUN |
| Same-company unmapped agent | NOT RUN |

The NOT RUN cases reach `HasPendingApproval` or `DispatchService.IsMappedAgent`, both of which query `ItsmDbContext`. Opening that context would use the application connection. This run did not do that, and it did not print a connection string. `AllowsTicketView` was not used for these results.

Unauthorized and missing handler downloads were not executed. An unauthenticated GET of `DownloadAttachment.ashx?id=999999` timed out at 20 seconds and again at 45 seconds. `login.aspx` returned HTTP 200 in the same period. IIS was not recycled. No password was used.

## Browser

Signed-in R1–R5 cases: **NOT RUN**. No authenticated session was available, and no password was read or entered.

Upload, authorized handler download, and a signed-in 404 for a missing or unauthorized attachment: **NOT RUN**.

## Defects

- High: the scoped ITSM `web.config` does not load under this IIS Express site. Direct GET returns HTTP 500.19 because the URL-authorization `add` duplicates an existing `users="*"` entry. The image is not disclosed, and the intended deny rule is not active.
- The Ticket Detail HTML-comment parser error is fixed on `4fff90f`.

## Remaining blocker

Change only `UploadedFiles/ITSM/web.config` so IIS can load it on this site, then GET the known file and record a denial that returns no file bytes. After that, run the same-company approver, permission-viewer, mapped-agent, and unmapped-agent `CanViewTicket` calls, and the signed-in handler and R1–R5 browser cases. Do not edit the root `Web.config`.
