# Security Admin v2.3 — Switch User UAT Pack

**Feature Branch:** `feature/security-admin-v2.3-switch-user`  
**Base Branch:** `Aug26_AILWorks_AmimnrupSupport`  
**Baseline:** v2.2.5-foundation-closure  
**Date:** September 11, 2026  

---

## Overview

This pack contains all UAT evidence for the Switch User feature. Follow the steps below to validate the feature in your local VS2015/IIS environment before merging.

## Prerequisites

1. VS2015 with the solution open
2. Local IIS configured with the AnmolDristi site
3. SQL Server connection to `AnmolDristiDev` database
4. At least one allowlisted employee code (e.g., `ADMIN`, `AHO445`) and one non-allowlisted code

## UAT Flow (10-15 minutes)

### Step 1: Build
1. Open `AnmolDristi.sln` in VS2015
2. Clean Solution
3. Rebuild Solution
4. Verify: **0 errors, 0 warnings** (see `BUILD_EVIDENCE.md`)

### Step 2: Deploy to IIS
1. Copy build output to IIS application folder
2. Recycle the application pool
3. Verify: Login page loads at `http://localhost/AnmolDristi/login.aspx` (see `IIS_VALIDATION.md`)

### Step 3: Login Validation
1. Login as a normal (non-allowlisted) user
2. Verify: Session keys populated correctly (see `SESSION_VALIDATION.md`)
3. Verify: "Switch User" link NOT visible in sidebar

### Step 4: Switch User Validation
1. Login as `ADMIN` (allowlisted)
2. Verify: "Switch User" link visible in sidebar
3. Click "Switch User" → verify page loads
4. Search by employee code (e.g., `S319`)
5. Search by employee name (e.g., `John`)
6. Click "Switch User" on a result
7. Verify: Impersonation banner appears on all pages
8. Verify: `Session["WORKMAN"]` now shows target employee code
9. Verify: `Session["IsImpersonating"]` = `"true"`
10. Verify: `Session["Original_WORKMAN"]` = `"ADMIN"`

### Step 5: Return Validation
1. Click "Return to My Account" in banner
2. Verify: Redirected to home.aspx
3. Verify: `Session["WORKMAN"]` = `"ADMIN"` again
4. Verify: `Session["IsImpersonating"]` removed
5. Verify: All `Original_*` keys removed

### Step 6: Security Validation
1. Login as non-allowlisted user → verify "Access Denied" page
2. Login as allowlisted → try to switch to self → verify denied
3. Login as allowlisted → switch to user → try to switch again → verify nested denied
4. Try switching to inactive employee → verify denied

### Step 7: Logout Validation
1. Login as `ADMIN` → switch to `S319`
2. Click Logout
3. Verify: Returns to original identity first, then logs out
4. Verify: `ActiveUserSession` row shows `IsActive=0` for original session

### Step 8: Audit Validation
1. Check `~/Logs/SwitchUser/switch_user_YYYY-MM-DD.log`
2. Verify: SWITCH_START event logged
3. Verify: SWITCH_RETURN event logged
4. Verify: Correlation maintained (same SessionID)

## Sign-Off

After completing all steps, update `FINAL_SIGNOFF.md` with GO or NO-GO.
