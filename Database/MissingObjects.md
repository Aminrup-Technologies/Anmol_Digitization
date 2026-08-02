# Missing Objects

Baseline extraction target: `AnmolDristiDev` (live SQL Server instance, `115.112.186.71`), extracted 2 August 2026.

This file lists every database object that application source code for the Monthly Sales Claims workflow references by name, but which could not be located in the target database. Per the extraction instructions, **no script has been generated for any object listed here.** Each entry states exactly where the reference was found and why the object appears to be absent, based only on what could be verified.

Verification method for each entry: a lookup against `sys.objects` for an exact, case-insensitive name match, plus a wildcard search (`LIKE '%name%'`) against `sys.objects` to rule out a rename. No near-match was found for any of the three objects below.

---

## 1. `usp_ProcessApprovalRequestDetails`

| Field | Detail |
| --- | --- |
| **Object Name** | `usp_ProcessApprovalRequestDetails` |
| **Expected Type** | Stored Procedure |
| **Referenced By** | `AnmolDristi/ClaimsApproval.aspx.cs`, line 2429 (`btnApproveSelected_Click_OLD1`), and line 2475 (`btnRejectSelected_Click_OLD1`) |
| **Reason it appears missing** | Both call sites are inside handlers whose method names are explicitly suffixed `_OLD1` and are not wired to any live control event on `ClaimsApproval.aspx` — they are dead code. A procedure named `usp_ProcessApprovalRequestDetails_WithRemarks` **is** present in the target database and **is** called from a live handler (`ProcessSelectedDetails`) in the same file. `usp_ProcessApprovalRequestDetails` (without the `_WithRemarks` suffix) appears to be the predecessor this procedure superseded, and was most likely dropped or renamed at that time. The dead code referencing the old name was never removed from the page. |

---

## 2. `usp_GetApprovalRequestsForUser`

| Field | Detail |
| --- | --- |
| **Object Name** | `usp_GetApprovalRequestsForUser` |
| **Expected Type** | Stored Procedure |
| **Referenced By** | `AnmolDristi/ClaimsSubmitted.aspx.cs`, line 48 |
| **Reason it appears missing** | `ClaimsSubmitted.aspx` is not linked from `Dristi.Master` or from any other page in the application (confirmed by a repository-wide search for `ClaimsSubmitted.aspx` outside its own three files). The page is unreachable through normal navigation. The procedure it calls may have been dropped, renamed during later refactoring, or never deployed to this environment; there is no way to distinguish between these possibilities from the current database state alone. |

---

## 3. `usp_GetApprovers_WithComputedState`

| Field | Detail |
| --- | --- |
| **Object Name** | `usp_GetApprovers_WithComputedState` |
| **Expected Type** | Stored Procedure |
| **Referenced By** | `AnmolDristi/ClaimsSubmitted.aspx.cs`, line 232 |
| **Reason it appears missing** | Same orphan page as above (`ClaimsSubmitted.aspx`), same reasoning: the page is unreachable, and the procedure it depends on is not present in the target database. |

---

## Summary

| # | Object | Type | Live in target DB? |
| --- | --- | --- | --- |
| 1 | `usp_ProcessApprovalRequestDetails` | Stored Procedure | No |
| 2 | `usp_GetApprovalRequestsForUser` | Stored Procedure | No |
| 3 | `usp_GetApprovers_WithComputedState` | Stored Procedure | No |

All three missing objects are referenced exclusively from code that is either explicitly marked as dead (`_OLD1` suffix) or from a page with no navigational path (`ClaimsSubmitted.aspx`). No object required by a **live, reachable** code path was found to be missing.
