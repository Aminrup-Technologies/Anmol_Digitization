# AUDIT_VALIDATION.md — Audit Trail Verification

**Date:** ___________  

## Log Location

```
~/Logs/SwitchUser/switch_user_YYYY-MM-DD.log
```

## Log Format

```
TIMESTAMP | Action=ACTION | OriginalEmp=CODE | TargetEmp=CODE | TargetName=NAME | User=NAME | SessionID=ID | IP=ADDRESS
```

## Validation Steps

### Step 1: Perform Switch
1. Login as `ADMIN`
2. Switch to `S319`
3. Check log file

**Expected entry:**
```
2026-09-11 HH:MM:SS | Action=SWITCH_START | OriginalEmp=ADMIN | TargetEmp=S319 | TargetName=... | User=... | SessionID=... | IP=...
```

- [ ] SWITCH_START event logged
- [ ] OriginalEmp = `ADMIN`
- [ ] TargetEmp = `S319`
- [ ] SessionID matches current session
- [ ] IP address captured

### Step 2: Perform Return
1. Click "Return to My Account"
2. Check log file

**Expected entry:**
```
2026-09-11 HH:MM:SS | Action=SWITCH_RETURN | OriginalEmp=ADMIN | TargetEmp=S319 | User=... | SessionID=... | IP=...
```

- [ ] SWITCH_RETURN event logged
- [ ] Same SessionID as SWITCH_START
- [ ] Timestamp after SWITCH_START

### Step 3: Correlation
- [ ] Both entries have same SessionID
- [ ] SWITCH_RETURN timestamp > SWITCH_START timestamp
- [ ] No duplicate entries for same action

### Step 4: Denied Access Audit
1. Login as non-allowlisted user
2. Try to access SwitchUser.aspx

**Expected entry:**
```
... | Action=DENY_NOT_AUTHORIZED | OriginalEmp=... | ...
```

- [ ] DENY event logged for unauthorized access

## Result

**PASS / FAIL:** ___________

**Notes:** ___________
