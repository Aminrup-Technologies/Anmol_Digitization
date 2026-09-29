# Regression Checklist — Sprint 1 — `VMS_SelfLogin`

Execute **after** IMP coding and **before** Approve → Commit.  
Environment: non-production (`AnmolDristiDev` / UAT) with a known `BranchId` that has `VMS_Menu`/timings configured.

**Invariants (every run):** OTP is 6 digits; DB `ExpiresAt` ≈ now+10 minutes; session keys `VerifiedMobile` / `VerifiedEmail` / `OTPAttempts` still used; redirects remain `VMS_MyVisits.aspx` and `VMS_Registration.aspx?BranchId=`.

---

## Master / visitor isolation

| # | Test | Setup | Action | Expected Result | IMP |
|---|------|-------|--------|-----------------|-----|
| RC-01 | Idle does not kick visitor | Open `VMS_SelfLogin.aspx?BranchId={valid}` on kiosk/tablet | Leave idle **> 10 minutes** (no mouse/key) | Page remains on SelfLogin (or same VMS visitor flow). Does **not** navigate to `login.aspx`. | IMP-01 |
| RC-02 | ERP idle still works | Login as employee on ERP page using Master | Idle **> 10 minutes** | Still redirects to `login.aspx` (ERP policy unchanged). | IMP-01 |
| RC-03 | Cross-tab logout isolation | Tab A: employee logged into ERP. Tab B: SelfLogin visitor page | Logout in Tab A | Tab B stays on SelfLogin; not forced to `login.aspx`. Tab A logs out normally. | IMP-02 |

---

## Core visitor workflow (must never break)

| # | Test | Setup | Action | Expected Result | IMP |
|---|------|-------|--------|-----------------|-----|
| RC-10 | Happy path — new visitor | Valid BranchId QR/URL; mobile with SMS | Send OTP → enter correct code → Proceed | OTP SMS received; verify succeeds; redirect to `VMS_Registration.aspx?BranchId={same}`. `Session["VerifiedMobile"]` set. | All OTP IMPs |
| RC-11 | Happy path — existing active visit | Mobile with Pending/Approved active visit at that branch | Complete OTP | Redirect to `VMS_MyVisits.aspx` (not Registration). | — |
| RC-12 | OTP format | Capture OTP from SMS or DB (dev only) | Inspect value | Exactly **6 numeric digits**. | IMP-04 |
| RC-13 | OTP expiry | Insert/send OTP; wait **> 10 minutes** (or set ExpiresAt past in Dev) | Enter that OTP | Rejected as invalid/expired. | — |
| RC-14 | Old OTP after resend | Send OTP #1; Resend OTP #2 | Enter OTP #1 | Fails after IMP-05. Enter OTP #2 succeeds. | IMP-05 |
| RC-15 | Resend throttle | On verify panel | Click Resend repeatedly within cooldown | Second send blocked with clear message; no burst SMS. | IMP-06 |
| RC-16 | SMS failure UX | Force SMS failure (bad config password in Dev) | Send OTP | User sees error; does **not** get false “sent” success (per IMP-07 policy). | IMP-07, IMP-03, IMP-11 |
| RC-17 | Missing BranchId | Open SelfLogin with no QueryString | Load page | “Invalid QR Code…”; OTP panel hidden. | — |
| RC-18 | Branch tamper | Complete send OTP; tamper `hfBranchId` before verify (browser tools) | Verify | Rejected or forced to server-trusted branch (per IMP-08). Must not silently register to attacker branch. | IMP-08 |
| RC-19 | Attempt lock | Enter wrong OTP 5 times | 6th verify | Locked with clear message. | IMP-09 |
| RC-20 | Attempts vs resend | Fail 4 times; Resend; enter wrong again / correct | Matches approved RF-09 A or B semantics exactly | IMP-09 |
| RC-21 | SMS template (if IMP-13 shipped) | Send OTP | Read SMS | Matches approved VMS DLT text (not CRM Portal) | IMP-13 |

---

## Negative / safety

| # | Test | Action | Expected |
|---|------|--------|----------|
| RC-30 | Invalid mobile (not 10 digits) | Send OTP | Validation error; no SMS |
| RC-31 | Page reload mid-OTP | Reload SelfLogin?BranchId= | New clean session; OTPAttempts cleared; must Send again |
| RC-32 | Registration still accepts VerifiedMobile | After successful OTP → Registration | Registration loads; mobile prefilled/locked as today |
| RC-33 | Email OTP UI gone (if IMP-10) | Inspect page | No email OTP input shown; mobile flow only |

---

## Sign-off

| Role | Name | Date | Result |
|------|------|------|--------|
| Implementer | | | Pass / Fail |
| ChatGPT review | | | Pass / Fail |
| Stakeholder Approve | | | Approved / Rejected |

**Commit allowed only when:** all executed RC rows for shipped IMPs = Pass, and Approve = Approved.
