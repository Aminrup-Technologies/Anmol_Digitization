# FINAL_SIGNOFF.md — Switch User v2.3 UAT Sign-Off

**Feature:** Security Admin v2.3 — Switch User  
**Branch:** `feature/security-admin-v2.3-switch-user`  
**Date:** ___________  
**UAT Performed By:** ___________  

---

## UAT Checklist

| # | Check | Status | Notes |
|---|-------|--------|-------|
| 1 | Clean → Rebuild (0 errors) | ⬜ | |
| 2 | IIS deployment successful | ⬜ | |
| 3 | Login page loads | ⬜ | |
| 4 | Normal login works | ⬜ | |
| 5 | Session keys populated correctly | ⬜ | |
| 6 | Non-allowlisted user: Switch User hidden | ⬜ | |
| 7 | Allowlisted user: Switch User visible | ⬜ | |
| 8 | Search by employee code | ⬜ | |
| 9 | Search by employee name | ⬜ | |
| 10 | Switch succeeds | ⬜ | |
| 11 | Impersonation banner visible | ⬜ | |
| 12 | Target permissions correct | ⬜ | |
| 13 | Return restores original session | ⬜ | |
| 14 | Nested impersonation blocked | ⬜ | |
| 15 | Self-switch blocked | ⬜ | |
| 16 | Inactive employee blocked | ⬜ | |
| 17 | Missing employee blocked | ⬜ | |
| 18 | Logout during impersonation works | ⬜ | |
| 19 | Audit log captures events | ⬜ | |
| 20 | No regression in existing features | ⬜ | |

---

## Decision

**☐ GO** — All checks pass. Safe to merge.

**☐ NO-GO** — Issues found. Do not merge.

---

## Blockers (if NO-GO)

1. ___________
2. ___________
3. ___________

---

## Signatures

**UAT Tester:** ___________  Date: ___________

**Tech Lead:** ___________  Date: ___________

---

## Rollback Plan

If issues are discovered after merge:

1. Revert the merge commit: `git revert -m 1 <merge-sha>`
2. Redeploy to IIS
3. Verify login/session flow returns to baseline

No database changes are required for rollback — all data is in-memory session state.
