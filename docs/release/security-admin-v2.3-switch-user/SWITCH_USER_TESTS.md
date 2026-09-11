# SWITCH_USER_TESTS.md — Switch User Test Matrix

**Date:** ___________  

## Test Scenarios

### T1: Search by Employee Code
**Precondition:** Logged in as allowlisted admin  
**Steps:**
1. Navigate to SwitchUser.aspx
2. Enter `S319` in search box
3. Click Search  

**Expected:** One result card showing `S319` with employee name  

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T2: Search by Employee Name
**Precondition:** Logged in as allowlisted admin  
**Steps:**
1. Navigate to SwitchUser.aspx
2. Enter `Rohan` in search box
3. Click Search  

**Expected:** Results showing employees with "Rohan" in name  

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T3: Switch User Succeeds
**Precondition:** Logged in as `ADMIN` (allowlisted)  
**Steps:**
1. Navigate to SwitchUser.aspx
2. Search for `S319`
3. Click "Switch User" button on the result  

**Expected:**
- Success message displayed
- Impersonation banner appears: "Impersonating: [Name] (S319) | Original User: [Admin Name] (ADMIN)"
- Sidebar shows impersonated user's menu items

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T4: Return to Original User
**Precondition:** Currently impersonating `S319`  
**Steps:**
1. Click "Return to My Account" in banner  

**Expected:**
- Redirected to home.aspx
- Banner disappears
- `Session["WORKMAN"]` = `"ADMIN"`
- `Session["IsImpersonating"]` = null
- All `Original_*` keys removed

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T5: Nested Impersonation Blocked
**Precondition:** Currently impersonating  
**Steps:**
1. Navigate to SwitchUser.aspx
2. Search for another employee
3. Click "Switch User"  

**Expected:** Error message: "Nested impersonation is not allowed."  

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T6: Self-Switch Blocked
**Precondition:** Logged in as `ADMIN`  
**Steps:**
1. Navigate to SwitchUser.aspx
2. Search for `ADMIN`
3. Click "Switch User"  

**Expected:** Error message: "You cannot switch to your own account."  

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T7: Unauthorized User Blocked
**Precondition:** Logged in as non-allowlisted user  
**Steps:**
1. Navigate to SwitchUser.aspx (direct URL)  

**Expected:** "Access Denied" page with lock icon  

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T8: Inactive Employee Blocked
**Precondition:** Logged in as allowlisted admin  
**Steps:**
1. Search for an inactive employee code  

**Expected:** No results (only Active employees returned)  

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T9: Missing Employee Blocked
**Precondition:** Logged in as allowlisted admin  
**Steps:**
1. Search for `NONEXISTENT123`  

**Expected:** "No active employees found" message  

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T10: Logout During Impersonation
**Precondition:** Currently impersonating `S319`  
**Steps:**
1. Click Logout  

**Expected:**
- Session returns to original identity first
- Then logs out normally
- `ActiveUserSession.IsActive = 0` for original session
- Redirected to login.aspx

**Result:** PASS / FAIL  
**Notes:** ___________

---

### T11: Feature Disabled Blocks Access
**Precondition:** `SwitchUserEnabled` = `"false"` in Web.config  
**Steps:**
1. Login as allowlisted admin
2. Navigate to SwitchUser.aspx  

**Expected:** Access denied (feature disabled)  

**Result:** PASS / FAIL  
**Notes:** ___________

---

## Summary

| Test | Result |
|------|--------|
| T1: Search by Code | |
| T2: Search by Name | |
| T3: Switch Succeeds | |
| T4: Return Restores | |
| T5: Nested Blocked | |
| T6: Self-Switch Blocked | |
| T7: Unauthorized Blocked | |
| T8: Inactive Blocked | |
| T9: Missing Blocked | |
| T10: Logout During Impersonation | |
| T11: Feature Disabled | |

**Overall:** PASS / FAIL
