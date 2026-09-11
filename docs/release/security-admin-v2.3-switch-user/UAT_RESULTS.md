# Switch User UAT Results

Canonical release candidate: **PR #217** (`cursor/switch-user-basepage-compile-ad85` → `Aug26_AILWorks_AmimnrupSupport`).

Security and session design: [Switch User Architecture](../../security/SWITCH_USER_ARCHITECTURE.md). Do not duplicate that document here.

## Environment

- Branch: `cursor/switch-user-basepage-compile-ad85`
- Head: `0291afedeb6bf889b8b750eccc646d1f415a0768`
- Base: `Aug26_AILWorks_AmimnrupSupport`
- Build: local VS2015 / IIS — successful
- Date: 2026-09-11
- Evidence: local UAT (screenshots retained by the release gatekeeper; not recreated in this PR)

## Test Matrix

| Test | Result | Evidence |
|------|--------|----------|
| Switch User page loads | PASS | Local UAT 2026-09-11 |
| Employee search | PASS | Local UAT 2026-09-11 |
| Switch User | PASS | Local UAT 2026-09-11 |
| Redirect to `home.aspx` | PASS | Local UAT 2026-09-11; `SwitchUser.aspx.cs` `Response.Redirect("~/home.aspx", false)` + `CompleteRequest()` |
| Sidebar refreshes immediately | PASS | Local UAT 2026-09-11; `RefreshUserPermissions()` + `RefreshChromeAfterIdentityChange()` then Home GET |
| Single Master impersonation banner | PASS | Local UAT 2026-09-11; page `pnlImpersonationBanner` stays hidden |
| Return to My Account | PASS | Local UAT 2026-09-11; Master `SwitchUser.aspx?action=return` |
| Original identity restored | PASS | Local UAT 2026-09-11; `ReturnToOriginalSession` |
| Build successful | PASS | Local VS2015 build |

## Security Validation

- **LoginToken preserved** — `HydrateTargetSession` does not write `LoginToken`; no `CreateActiveSession` in `SwitchUserHelper.cs`.
- **ActiveSessionId preserved** — same; identity keys only.
- **PH_UserPermSet refresh** — `session.Remove("PH_UserPermSet")` after hydrate; `RefreshUserPermissions()` after switch.
- **Nested impersonation blocked** — `IsCurrentlyImpersonating` → `DENY_NESTED`.
- **Self-switch blocked** — caller vs target, ordinal ignore-case.
- **Feature toggle validated** — `SwitchUserEnabled` fail-closed; menu hidden when disabled. Allowlist: `SwitchUserAuthorizedUsers`.

## Final Sign-off

**Release Candidate Accepted**

PR #217 is the canonical Switch User RC for merge into `Aug26_AILWorks_AmimnrupSupport`. PR #216 is superseded (wrong base: `main`, no `BasePage`).
