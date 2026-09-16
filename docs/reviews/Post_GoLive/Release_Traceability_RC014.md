# Release Traceability — RC-014B / RC-014D

**Captured:** 2026-09-15  
**Constraint:** documentation and release governance only. No application code, SQL, Git history, tags, or deployments.  
**Closes:** the audit gap recorded in `docs/reviews/Post_GoLive/Post_GoLive_Baseline.md` — frozen tag `94bc2089` vs UAT-certified RC-014B/D in the working tree and live UAT.

| Item | Value |
| --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| Frozen commit | `94bc2089c0a43d38619a4df2bfff139269b7598c` |
| Local tag | `tada-v1.0-go-live-2026` (not pushed) |
| Tag package | RC-001 through RC-013 |
| Certified product | RC-001 through RC-014D |

---

## 1. Release Traceability Matrix

| Artifact | RC-001–RC-013 | RC-014B | RC-014D |
| --- | --- | --- | --- |
| Release tag `94bc2089` | Present | Missing | Missing |
| Working tree | Present | Present | Present |
| Live UAT SQL | Present | Present | Present |
| Live UAT IIS | Present | Present | Present |
| UAT evidence | Present | Request #97 | Request #97 |

How to read the matrix:

- **Release tag `94bc2089`:** commit subject `feat(tada): production go-live hardening (RC-001–RC-013)`. Local tag `tada-v1.0-go-live-2026` is the named handle for that SHA. RC-014B/D are absent from the object.
- **Working tree:** HEAD remains `94bc2089`; RC-014B/D exist as uncommitted files against that SHA.
- **Live UAT SQL:** `anmol_uat` → `AnmolDristiDev`. RC-001–RC-013 procedures per Go-Live Certification §3. RC-014B `usp_ProcessApprovalAction` `modify_date` 2026-09-15 18:04:06. RC-014D `usp_MarkRequestReadyForPayment` `modify_date` 2026-09-15 21:50:31.
- **Live UAT IIS:** local IIS Express `http://localhost:53713` serving `AnmolDristi`. RC-001–RC-013 proven in `TA_DA_Smoke_Test_Execution.md`. RC-014B proven with that site on Request #97. RC-014D proven after `AnmolDristi.dll` 2026-09-15 21:54:56 AppDomain recycle on the same site.
- **UAT evidence:** RC-001–RC-013 → `docs/releases/TA_DA_GoLive_2026/`. RC-014B/D → Request #97 packs under `docs/reviews/L2_Auditor_AsIs/`.

RC-014A (Request #96, FAIL) and RC-014C (Request #95, FAIL) are superseded proofs, not columns in this matrix.

---

## 2. Evidence Chain

Summaries only. Full tables and screenshots remain in the cited packs.

### 2.1 RC-001–RC-013

| Source | What it certifies |
| --- | --- |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_GoLive_Certification.md` | RC-001–RC-013 PASS; implementation freeze at the tagged package |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_Smoke_Test_Execution.md` | Request 96 Submit / Return / KM / RESUBMIT / Forward / modal auto-close |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md` | Production packet for the tagged SHA (does not list RC-014 SPs) |

### 2.2 RC-014B

**Source:** `docs/reviews/L2_Auditor_AsIs/RC014B_RC014D_UAT_Request97.md`  
**Verdict in that pack:** RC-014B PASS (RC-014D FAIL on the same pass; closed later by §2.3).

| Fact | Value |
| --- | --- |
| Claim | Request **97** (Ajay Kumar Sahu / `AHS1454`, August 2026) |
| Actor | `Audit` on `http://localhost:53713/ClaimsApproval_v2.aspx` |
| Live SP | `usp_ProcessApprovalAction` `modify_date` **2026-09-15 18:04:06** |
| Header | Remained `Pending` through first, second, and bulk-except-last Auditor approve |
| Last latest activity | Header `Pending` → **`Completed`**; routing NULL; Auditor row Approved |
| History | **687** `Pending → Completed`, Remarks `Final Auditor Approval` |
| Queue | Request 97 left View Pending; **94** remained Pending |

RC-014A on Request 96 proved the pre-014B defect (first Auditor day wrote `Completed`). Request 97 is the certified gatekeeper proof.

### 2.3 RC-014D

**Source:** `docs/reviews/L2_Auditor_AsIs/RC014D_Deployment_Validation.md`  
**Prior FAIL:** same Request 97 Confirm threw closed-connection; live MarkReady still positional (RC-014C error 213 on Request 95).  
**Verdict:** RC-014D PASS.

| Fact | Value |
| --- | --- |
| Claim | Request **97** (parked at `Completed` after RC-014B) |
| DLL | `AnmolDristi.dll` **2026-09-15 21:54:56**; IL `Open()` then `usp_MarkRequestReadyForPayment` |
| SP | `usp_MarkRequestReadyForPayment` `modify_date` **2026-09-15 21:50:31**; named-column history INSERT |
| Header | **`Completed → ReadyForPayment`** |
| History | **688** `Completed → ReadyForPayment`, Remarks `Auditor marked Ready for Payment` |
| Queue cleanup | 97 absent from View Pending; 94 still Pending; Approved Claims shows 97 `ReadyForPayment` |
| Errors not seen | closed-connection; SQL 213 |

`usp_ProcessApprovalAction` was not altered in the RC-014D deploy (`modify_date` remained 18:04:06). No Git commit was created in that validation (constraint in the pack).

---

## 3. Repository Delta

Exactly three paths separate frozen `94bc2089` from the certified environment. No diffs in this file.

| Path | RC | Why it changed | Where proven | Why excluded from `94bc2089` |
| --- | --- | --- | --- | --- |
| `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` | RC-014B | After L1 Forward, latest days are already `Approved` at level 1. Counting only Pending/Submitted made unresolved = 0, so the first Auditor approve wrote `Completed` (RC-014A). Unresolved now includes `Approved` with `DetailApprovedLevel <` current. Last Auditor day writes `Completed` and history `Final Auditor Approval` so MarkReady is reachable. | Request **97**; SP 18:04:06; History **687**. `RC014B_RC014D_UAT_Request97.md` | Commit `94bc2089` is the RC-001–RC-013 go-live freeze (15:51). Auditor terminal work started after that freeze. RC-014B is a post-tag certification, not part of the tagged package. |
| `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` | RC-014D | Positional `INSERT INTO ApprovalRequestStatusHistory VALUES (...)` failed after the table grew to 12 columns (RC-014C error 213). History write uses a named column list. | Request **97**; SP 21:50:31; History **688**. `RC014D_Deployment_Validation.md`; contract in `RC014C_MarkReady_Contract.md` | Same freeze: MarkReady named INSERT did not exist at tag time. Live UAT at RC-014B still had `modify_date` 2026-01-09. RC-014D deploy explicitly forbade additional repository commits. |
| `AnmolDristi/ClaimsApproval_v2.aspx.cs` | RC-014D | `btnMarkReadyForPayment_Click` called `ExecuteNonQuery` on a closed connection (`con.Open()` at ~2158). Confirm never reached the SP. | Request **97**; DLL 21:54:56; no closed-connection on retry. `RC014D_Deployment_Validation.md` | Source restore happened after the tag. The DLL that ran the first Request 97 Confirm was 17:13:50 (pre-Open). RC-014D validation rebuilt and copied the DLL but did not commit. |

No other application or SQL path is required to close tag-vs-certified for RC-014B/D. Adjacent docs under `docs/reviews/` are evidence, not product delta.

---

## 4. Future Freeze Instructions

Record only. **Do not execute** in this task.

When a controlled follow-up release is authorized:

1. Stage RC-014B files.  
   `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql`
2. Stage RC-014D files.  
   `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql`  
   `AnmolDristi/ClaimsApproval_v2.aspx.cs`
3. Create a follow-up release commit on `Aug26_AILWorks_AmimnrupSupport` (do not amend `94bc2089`). Message should state RC-014B + RC-014D Auditor close, not a rewrite of RC-001–RC-013.
4. Create a new annotated tag (do not move `tada-v1.0-go-live-2026`). Suggested pattern: a distinct post-go-live tag pointing at the new SHA.
5. Update `docs/reviews/Post_GoLive/Post_GoLive_Baseline.md` so Release identity lists the new SHA/tag and this file’s matrix cells for “Release tag” become Present for RC-014B and RC-014D.

Also update `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md` in that same freeze so production cannot ship `94bc2089` without the two RC-014 procedures and the rebuilt DLL. That checklist edit is not this task.

Until those five steps run, treat `94bc2089` as historical packaging and the working tree + live UAT as the certified runtime.

---

## 5. Sprint 3 Entry Gate

RC-001 through RC-014D are UAT-certified.

The release tag (`94bc2089` / `tada-v1.0-go-live-2026`) intentionally predates RC-014B and RC-014D. That is a packaging fact, not a product defect. The tagged SHA remains the RC-001–RC-013 go-live object. The certified Auditor close lives in the working tree and on live UAT (Request #97, History 687 then 688).

Future enhancements must branch from the certified RC-014 working state — `usp_ProcessApprovalAction` (RC-014B), `usp_MarkRequestReadyForPayment` plus `ClaimsApproval_v2.aspx.cs` `con.Open()` (RC-014D) — not from the historical tag alone. Shipping or branching from `94bc2089` without that delta reopens RC-014A (premature `Completed`) and RC-014C/D (Ready for Payment never persists).

Sprint 3 may start. The first product RC against this gate is a new enhancement, not another close of RC-014B/D.
