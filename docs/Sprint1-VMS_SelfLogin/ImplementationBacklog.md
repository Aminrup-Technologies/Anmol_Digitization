# Implementation Backlog — Sprint 1 — `VMS_SelfLogin` (IMP-xx)

**Status:** Drafted from RF-xx. **Do not start coding until Analysis is Approved.**

Each IMP maps 1:1 (or grouped) to RF items. Implementation must preserve OTP = 6 digits, expiry = 10 minutes, session key names, URLs, and visitor routing.

---

## Suggested implementation order

```
IMP-01 (RF-01) ──┐
IMP-02 (RF-02) ──┼── Master visitor isolation (ship together)
                 │
IMP-03 (RF-03) ──── SMS credentials to config
IMP-04 (RF-04) ──── CSPRNG OTP (same 6-digit contract)
IMP-05 (RF-05) ──── Invalidate prior OTPs on send
IMP-06 (RF-06) ──── Send/resend throttle
IMP-07 (RF-07) ──── SMS failure UX
IMP-08 (RF-08) ──── BranchId integrity
IMP-09 (RF-09) ──── OTPAttempts semantics
IMP-10 (RF-10) ──── Dead email OTP cleanup
IMP-11 (RF-14) ──── SMS timeout (can bundle with IMP-03/07)
IMP-12 (RF-13) ──── Constants hygiene (optional / last)
IMP-13 (RF-11) ──── DLT template (blocked on compliance)
IMP-14 (RF-12) ──── Design note only — NO CODE
```

---

## IMP Catalogue

### IMP-01 — Disable idle auto-logout on visitor pages  
**RF:** RF-01 · **Files:** `Dristi.Master` (and/or `Dristi.Master.cs` if server flag preferred)  
**Do:** Skip `startIdleTimer` / idle redirect on visitor public pages.  
**Do not:** Change timer duration for ERP pages; change OTP logic.  
**Done when:** On SelfLogin, 10+ minutes idle does **not** navigate to `login.aspx`. ERP pages still idle-logout.  
**Regression:** RC-01, RC-02  

---

### IMP-02 — Ignore cross-tab `app_logout` on visitor pages  
**RF:** RF-02 · **Files:** `Dristi.Master`  
**Do:** Skip `handleRemoteLogout` redirect/close on visitor public pages.  
**Do not:** Break multi-tab logout for ERP modules.  
**Done when:** ERP logout in Tab A does not force SelfLogin Tab B to `login.aspx`.  
**Regression:** RC-03  

---

### IMP-03 — Externalize SMS credentials  
**RF:** RF-03 · **Files:** `VMS_SelfLogin.aspx.cs`, `Web.config`, transforms as needed  
**Do:** Read user/password/sid from config; remove literals from code-behind.  
**Do not:** Change OTP message body (unless IMP-13 approved); change API host without approval.  
**Done when:** SMS still delivers OTP; no secrets in `.cs`.  
**Regression:** RC-10, RC-11  

---

### IMP-04 — Cryptographic OTP generation (6 digits preserved)  
**RF:** RF-04 · **Files:** `VMS_SelfLogin.aspx.cs`  
**Do:** CSPRNG → integer in `[100000, 999999]`; still store as string OTP.  
**Do not:** Change length, add letters, change expiry SQL.  
**Done when:** OTP always 6 numeric digits; verify path unchanged.  
**Regression:** RC-10, RC-12  

---

### IMP-05 — Invalidate prior OTPs on send  
**RF:** RF-05 · **Files:** `VMS_SelfLogin.aspx.cs`  
**Do:** Before/with INSERT, invalidate prior unverified rows for same mobile (e.g. set `IsVerified=1` or `ExpiresAt=past`) using existing columns only.  
**Do not:** ALTER TABLE.  
**Done when:** Only the latest OTP verifies; old codes fail.  
**Regression:** RC-13, RC-14  

---

### IMP-06 — Throttle Send / Resend  
**RF:** RF-06 · **Files:** `VMS_SelfLogin.aspx.cs` (session and/or DB timestamp of last send)  
**Do:** Enforce cooldown (proposed default 30–60s) and/or max N sends per mobile per window.  
**Do not:** Change OTP length/expiry.  
**Done when:** Rapid resend shows clear wait message; no extra SMS.  
**Regression:** RC-15  

---

### IMP-07 — SMS failure handling  
**RF:** RF-07 · **Files:** `VMS_SelfLogin.aspx.cs`  
**Do:** Detect SMS failure; show error; prefer **not** showing verify panel on hard failure. Align OTP row cleanup with chosen policy.  
**Do not:** Swallow errors silently.  
**Done when:** Simulated SMS failure does not claim “code sent” as success.  
**Regression:** RC-16  

---

### IMP-08 — BranchId integrity after OTP  
**RF:** RF-08 · **Files:** `VMS_SelfLogin.aspx.cs` (+ Registration only if additive contract approved)  
**Do:** Ensure post-verify BranchId cannot be silently swapped via HiddenField tampering; validate branch exists. Additive session key only if approved in Analysis sign-off.  
**Do not:** Rename `VerifiedMobile` / `VerifiedEmail` / `OTPAttempts`.  
**Done when:** Tampered `hfBranchId` on verify is rejected or overridden by server-trusted landing value.  
**Regression:** RC-17, RC-18  

---

### IMP-09 — OTPAttempts vs Resend semantics  
**RF:** RF-09 · **Files:** `VMS_SelfLogin.aspx.cs`  
**Do:** Implement **approved** choice A (reset attempts on send/resend) or B (message requires reload).  
**Do not:** Remove the 5-attempt guard entirely.  
**Done when:** Visitor messaging matches actual lock behaviour.  
**Regression:** RC-19, RC-20  

---

### IMP-10 — Remove dead email OTP surface  
**RF:** RF-10 · **Files:** `VMS_SelfLogin.aspx`, `.aspx.cs`, `.aspx.designer.cs`  
**Do:** Remove unused email OTP controls/method/designer drift.  
**Do not:** Touch mobile OTP panels/flow.  
**Done when:** Page compiles; mobile OTP UI unchanged.  
**Regression:** RC-10  

---

### IMP-11 — SMS HTTP timeout  
**RF:** RF-14 · **Files:** `VMS_SelfLogin.aspx.cs`  
**Do:** Explicit timeout on SMS request.  
**Bundle with:** IMP-03 / IMP-07 preferred.  
**Regression:** RC-16  

---

### IMP-12 — Named constants (values unchanged)  
**RF:** RF-13 · **Files:** `VMS_SelfLogin.aspx.cs`  
**Do:** Constants for attempt max=5, expiry minutes=10, OTP digit contract.  
**Do not:** Change numeric values.  
**Regression:** smoke RC-10  

---

### IMP-13 — VMS SMS DLT template  
**RF:** RF-11 · **Blocked on:** compliance/DLT approval  
**Do:** Replace CRM Portal wording with approved VMS template.  
**Do not:** Ship without DLT sign-off.  
**Regression:** RC-21  

---

### IMP-14 — Office hours / next-day design only  
**RF:** RF-12 · **Code:** **None**  
**Do:** Keep insertion map in GapAnalysis; no UI/workflow code in Sprint-1.  
**Done when:** Explicitly marked deferred in Approval notes.  

---

## Per-IMP Implementation Notes template

Copy into `ImplementationNotes.md` when coding starts:

```
### IMP-xx — <title>
- RF: 
- Branch: 
- Files touched: 
- Behaviour preserved: 
- Behaviour changed: 
- Config keys added: 
- ChatGPT review: Pending | Pass | Fail
- Regression: Pending | Pass | Fail
- Approve: Pending | Approved
- Commit SHA: 
```

---

## Unlock rule

| Condition | Action |
|-----------|--------|
| GapAnalysis RF pack not approved | No IMP coding |
| RF-01/02 approved | May start IMP-01/02 |
| RF-03–10 approved | May start corresponding IMPs |
| IMP coded + ChatGPT review + Regression Pass + Approve | Commit + push |
