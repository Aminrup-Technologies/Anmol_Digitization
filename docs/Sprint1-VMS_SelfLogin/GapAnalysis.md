# Gap Analysis — Sprint 1 — `VMS_SelfLogin` (RF-xx)

Compares live `VMS_SelfLogin.aspx` / `.aspx.cs` (and Master visitor-isolation risks) against the approved Sprint-1 modernization goals and hard constraints.

**Constraints:** OTP = 6 digits; expiry = 10 minutes; session key names unchanged; URLs unchanged; no schema change; workflow unchanged.

---

## RF Catalogue

| ID | Rank | Current Behaviour | Required Behaviour | Implementation Required | Risk |
|----|------|-------------------|--------------------|-------------------------|------|
| **RF-01** | Critical | `Dristi.Master` idle timer always starts; after 10 minutes idle → `location.href = 'login.aspx'`, including on `VMS_SelfLogin`. | Master auto-logout must **never** interrupt visitor experience on public VMS pages. | Gate idle timer (and related countdown UI that assumes ERP labels) so it does **not** run on visitor public pages: at minimum `vms_selflogin.aspx`; align with existing Master USERID exemption list (`vms_registration`, `vms_myvisits`, `vms_gatescanner`). | **Medium** for ERP idle policy if page detection is wrong; **Low** for OTP math. |
| **RF-02** | Critical | `localStorage.app_logout` storage handler on Master runs `handleRemoteLogout` → close/redirect `login.aspx` on **all** Master pages, including visitor SelfLogin. | Employee logout in another tab must not abort visitor OTP/kiosk flow. | On visitor public pages, skip remote-logout redirect/close (may still clear `ActiveLoginToken` if present). | **Low–Medium**. Multi-tab ERP logout semantics change only on VMS public pages. |
| **RF-03** | Critical | SMSCountry username/password/sid hardcoded in `SendOtpSms`. | Credentials must not live in page source; delivery behaviour unchanged until DLT/copy change is separately approved. | Move credentials to `Web.config` / transforms (or existing secret pattern); `SendOtpSms` reads config. Keep same endpoint, same message body, same OTP payload for this IMP unless RF-11 is approved. | **Medium**. Misconfigured env breaks SMS. |
| **RF-04** | High | OTP generated with `System.Random`. | Keep **6 numeric digits**; strengthen entropy. | Replace with cryptographically secure RNG mapped to range `100000–999999` only. | **Low**. |
| **RF-05** | High | Each Send/Resend INSERTs a new OTP; prior unexpired unverified rows remain valid. | Only one active OTP (or verify latest only) per mobile for the send window. | On send, invalidate prior active rows for that mobile **without** changing new OTP length/expiry (10 min). No schema change if existing columns suffice (`IsVerified` / ExpiresAt). | **Low–Medium**. |
| **RF-06** | High | Unlimited Send/Resend. | Throttle abuse without changing OTP rules. | Cooldown and/or max sends per mobile (and optionally session/IP) before allowing another SMS. | **Low–Medium** (legit impatient resend). |
| **RF-07** | High | SMS API failures swallowed; UI always shows “Verification code sent…”. | Visitor must not be told success when SMS failed (product: fail closed preferred). | Propagate SMS failure to UI; do not advance to verify panel on hard failure (or advance with explicit warning — prefer fail closed). Keep OTP row policy consistent with chosen UX. | **Medium** UX change. |
| **RF-08** | High | Post-OTP routing trusts `hfBranchId` (client-postable); OTP rows not bound to BranchId. | Branch used for `HasActiveVisit` / Registration must match the QR landing branch. | Server-side integrity: validate BranchId exists; prefer binding landing BranchId in a way that cannot be silently swapped on postback. **Do not rename** existing session keys; if a new key is required, document it in IMP notes and get approval (additive only). | **Medium**. |
| **RF-09** | Medium | After 5 failed verifies, message says request a new code; Resend does **not** reset `OTPAttempts`. | Attempt semantics must be honest and intentional. | Product choice A: reset `OTPAttempts` on successful Send/Resend; or B: keep lock and change message to require page reload. Document choice in IMP. | **Medium** (security vs UX). |
| **RF-10** | Medium | Dead email OTP UI (`pnlEmailOTPInput`), dead `QueueOtpEmail`, stale designer `txtEmail`. | Page surface matches live mobile-OTP-only flow. | Remove unused markup/method/designer drift. No behaviour change to mobile OTP. | **Low**. |
| **RF-11** | Medium | SMS body says “Anmol CRM Portal”. | VMS-appropriate DLT-approved template. | Change copy **only after** DLT/compliance sign-off. May be deferred. | **High** if changed without DLT. |
| **RF-12** | Medium (design-only) | SelfLogin does not show Open/Closed/Outside hours; no today vs next-day choice. | Future: inform visitor of hours via `VMS_PlantTimings` / `VmsHelper.IsFacilityOpen`; later allow today vs next working day. | **Analysis/design only this sprint:** document insertion points. **Do not implement** workflow change in Sprint-1 unless explicitly expanded. | N/A until scoped. |
| **RF-13** | Low | Magic numbers inline; `Debug.WriteLine` only. | Named constants (values unchanged); better ops logging later. | Optional hygiene constants for 5 / 10 / 6; logging can defer. | **Low**. |
| **RF-14** | Low | No explicit SMS HTTP timeout. | Bound SMS call duration. | Set explicit request timeout on SMS HTTP call. | **Low**. |

---

## Disposition summary

| Disposition | Count | IDs |
|-------------|------:|-----|
| Critical — implement first (Master / secrets) | 3 | RF-01, RF-02, RF-03 |
| High — OTP hardening (preserve 6-digit / 10-min contract) | 5 | RF-04, RF-05, RF-06, RF-07, RF-08 |
| Medium — UX honesty / hygiene / deferred DLT | 3 | RF-09, RF-10, RF-11 |
| Design-only (no code this sprint unless expanded) | 1 | RF-12 |
| Low | 2 | RF-13, RF-14 |

---

## Explicit non-gaps (already compliant / out of scope)

| Item | Notes |
|------|-------|
| OTP length 6 | `Random.Next(100000, 999999)` + `MaxLength="6"` — **preserve** |
| OTP expiry 10 minutes | `DATEADD(minute, 10, GETDATE())` — **preserve** |
| Redirect targets | `VMS_MyVisits.aspx` / `VMS_Registration.aspx?BranchId=` — **preserve** |
| Session key names | `VerifiedMobile`, `VerifiedEmail`, `OTPAttempts` — **preserve** |
| Parameterized SQL | Already used — keep |
| Schema | No CREATE/ALTER required for RF-01–RF-10, RF-13–RF-14 |
| MVC / Core migration | Out of scope |

---

## RF-12 Insertion map (design only)

```
Page_Load after LoadBranchName(branchId)
  ★ Future: VmsHelper.IsFacilityOpen → banner Open | Closed | Outside hours
    (non-blocking unless product later mandates block)

btnVerifyOTP_Click after VerifiedMobile set + BranchId parsed
  ★ Future: if outside hours → Today vs Next Working Day choice
    then HasActiveVisit → MyVisits | Registration
```

Reuse existing `VmsHelper.IsFacilityOpen` (already used by Registration) to avoid split-brain rules.

---

## Approval checklist for this Analysis pack

- [ ] RF-01 / RF-02 Master visitor isolation approach approved
- [ ] RF-03 config key names approved
- [ ] RF-05 invalidation strategy approved (no schema)
- [ ] RF-07 fail-closed vs warn-and-continue chosen
- [ ] RF-08 additive session key (if any) approved
- [ ] RF-09 A vs B chosen
- [ ] RF-11 deferred or DLT-ready
- [ ] RF-12 confirmed design-only for Sprint-1
- [ ] ChatGPT review of this Gap Analysis complete
- [ ] Stakeholder **Approve Analysis** → unlock IMP-xx
