# SESSION_VALIDATION.md — Session Keys Validation

**Date:** ___________  

## Pre-Switch Session (Admin Login)

After login as `ADMIN`, verify all session keys are populated:

| Key | Expected Value | Actual | ✓/✗ |
|-----|---------------|--------|-----|
| `CurrentUserId` | numeric int | | |
| `WORKMAN` | `"ADMIN"` | | |
| `USERFNAME` | first name | | |
| `USERNAME` | full name | | |
| `USERID` | numeric (same as CurrentUserId) | | |
| `REGION` | string | | |
| `BRANCH` | string | | |
| `MODULE` | string | | |
| `PLANTID` | string | | |
| `U_SITE` | string | | |
| `U_SITECODE` | string | | |
| `DESG` | string | | |
| `GRADE` | string | | |
| `RONE_NAME` | string | | |
| `RONE_CODE` | string | | |
| `RTWO_NAME` | string | | |
| `RTWO_CODE` | string | | |
| `AUDITOR_NAME` | `"Rohan Saraf"` | | |
| `AUDITOR_CODE` | `"Audit"` | | |
| `ERPPlantId` | int (0 if no plant) | | |
| `ActiveSessionId` | int > 0 | | |
| `LoginToken` | GUID string | | |
| `PH_UserPermSet` | HashSet\<string\> | | |
| `IsImpersonating` | null (not set) | | |

## Post-Switch Session (After Switching to Target)

After switching to target employee, verify:

| Key | Expected | Actual | ✓/✗ |
|-----|----------|--------|-----|
| `WORKMAN` | target employee code | | |
| `USERNAME` | target employee name | | |
| `IsImpersonating` | `"true"` | | |
| `Original_WORKMAN` | `"ADMIN"` | | |
| `Original_USERNAME` | admin full name | | |
| `Original_CurrentUserId` | admin's numeric ID | | |
| `ActiveSessionId` | unchanged (original) | | |
| `LoginToken` | unchanged (original) | | |

## Post-Return Session (After Return)

After clicking "Return to My Account":

| Key | Expected | Actual | ✓/✗ |
|-----|----------|--------|-----|
| `WORKMAN` | `"ADMIN"` (restored) | | |
| `USERNAME` | admin full name (restored) | | |
| `IsImpersonating` | null (removed) | | |
| `Original_*` keys | all removed | | |
| `ActiveSessionId` | unchanged | | |
| `LoginToken` | unchanged | | |
| `PH_UserPermSet` | admin's permissions (restored) | | |

## Result

**PASS / FAIL:** ___________

**Notes:** ___________
