# Switch User Architecture Document

**Version:** 2.3  
**Date:** September 11, 2026  
**Status:** Architecture Validated  
**Baseline:** v2.2.5-foundation-closure  

---

## 1. Existing Authentication Pipeline

### Login Flow
```
login.aspx → btn_signin_Click → ProcessSignIn()
  ↓
UserAuthService.AuthenticateAndMigrateIfNeeded(employeeCode, password)
  ↓ (queries MST_UserMaster by EmployeeCode, verifies PBKDF2 hash)
DataRow row returned (or null)
  ↓
Check EmployeeStatus == "Active"
  ↓
Session.Clear() + set all session keys from row
  ↓
SessionHelper.CreateOrReuseActiveSession(userId, employeeCode)
  ↓ (INSERT/UPDATE ActiveUserSession → returns ActiveSessionId + LoginToken GUID)
Set Session["ActiveSessionId"] + Session["LoginToken"]
  ↓
UpdateUserLastLogin(userId)
  ↓
Response.Redirect("home.aspx")
```

### Session Keys (verified from login.aspx.cs ProcessSignIn)
| Key | Source | Type |
|-----|--------|------|
| `CurrentUserId` | `MST_UserMaster.Id` | int |
| `WORKMAN` | `EmployeeCode` | string |
| `USERFNAME` | First token of `EmployeeName` | string |
| `USERNAME` | `EmployeeName` | string |
| `USERID` | `Id` (numeric) | int |
| `REGION` | `RegionId` | string |
| `BRANCH` | `BranchId` | string |
| `MODULE` | `Module` | string |
| `PLANTID` | `PlantId` | string |
| `U_SITE` | `DepartmentId` | string |
| `U_SITECODE` | `SubDepartmentId` | string |
| `DESG` | `Designation_ID` | string |
| `GRADE` | `GradeId` | string |
| `RONE_NAME` | `ReportingManager` | string |
| `RONE_CODE` | `ReportingManagerCode` | string |
| `RTWO_NAME` | `ReportingManagersManagerName` | string |
| `RTWO_CODE` | `ReportingManagersManagerCode` | string |
| `AUDITOR_NAME` | Hardcoded "Rohan Saraf" | string |
| `AUDITOR_CODE` | Hardcoded "Audit" | string |
| `ERPPlantId` | Computed from MST_Plant join | int |
| `ActiveSessionId` | `ActiveUserSession.ActiveSessionId` | int |
| `LoginToken` | `ActiveUserSession.LoginToken` (GUID) | string |
| `PH_UserPermSet` | `UserRole` → `RolePermission` → `Permission` | HashSet\<string\> |

### Logout Flow
```
btn_Logout_Click (Dristi.Master)
  ↓
SessionHelper.EndActiveSession(LoginToken)
  ↓ (UPDATE ActiveUserSession SET IsActive=0, LogoutTime=GETDATE())
  ↓ (UPDATE MST_UserMaster SET LastLogoutDate=GETDATE())
Session.Clear() + Session.Abandon()
  ↓
Expire ASP.NET_SessionId cookie
  ↓
Client-side: clear sessionStorage, broadcast localStorage, redirect to login.aspx
```

### Session Validation
- `BasePage.OnInit()` → `EnsureSessionValid()`
- Heartbeat: every 60s via `SessionHelper.UpdateHeartbeat(LoginToken)`
- Soft validation: every 5 min via `SessionHelper.IsSessionActive(LoginToken)`
- Stale cleanup: `Global.Application_Start` timer → `SessionHelper.CleanupStaleSessions(30)` every 5 min

### Permission Loading
- `UserRole` → `RolePermission` → `Permission` table chain
- Cached in `Session["PH_UserPermSet"]` as `HashSet<string>`
- `BasePage.HasPermission(key)` checks set membership
- `PermissionHelper.ApplyUiPermissions()` traverses control tree matching `UI.<ControlID>.View`

---

## 2. Switch User Design

### Principle
The impersonation system swaps the in-memory session identity. It does NOT create a second `ActiveUserSession` record, does NOT call `UserAuthService`, and does NOT touch the login pipeline. The existing `LoginToken` and `ActiveSessionId` remain unchanged.

### Session Capture (Before Impersonation)
All auth session keys are copied to `Original_*` prefixed keys:
```
Session["Original_WORKMAN"] = Session["WORKMAN"]
Session["Original_USERNAME"] = Session["USERNAME"]
... (all 22 keys)
Session["Original_PH_UserPermSet"] = deep copy of permission HashSet
Session["IsImpersonating"] = "true"
```

### Session Hydration (Target User)
Session is overwritten with the target employee's identity:
```
Session["WORKMAN"] = targetRow["EmployeeCode"]
Session["USERNAME"] = targetRow["EmployeeName"]
... (all keys)
Session.Remove("PH_UserPermSet") // force reload for target user's permissions
```

ERP Plant ID is resolved using the same join query from login.aspx.cs.

### Return to Original User
All `Original_*` keys are restored:
```
foreach key in AuthSessionKeys:
    Session[key] = Session["Original_" + key]
    Session.Remove("Original_" + key)
Session["PH_UserPermSet"] = restored HashSet
Session.Remove("IsImpersonating")
```

### Authorization Rules
| Condition | Result |
|-----------|--------|
| User not logged in (`WORKMAN` null) | Deny |
| Employee not in `SwitchUserAuthorizedUsers` allowlist | Deny |
| Already impersonating | Deny (no nested impersonation) |
| Target inactive | Deny |
| Target missing | Deny |
| Self-switch | Deny |
| Feature disabled | Deny |

Fail-closed: any error → deny.

### Audit Trail
Logs to `~/Logs/SwitchUser/switch_user_YYYY-MM-DD.log`:
```
2026-09-11 14:30:00 | Action=SWITCH_START | OriginalEmp=ADMIN | TargetEmp=S319 | TargetName=John Doe | User=Admin User | SessionID=abc123 | IP=192.168.1.100
2026-09-11 14:45:00 | Action=SWITCH_RETURN | OriginalEmp=ADMIN | TargetEmp=S319 | User=Admin User | SessionID=abc123 | IP=192.168.1.100
```

---

## 3. Files Changed

### New Files
| File | Purpose |
|------|---------|
| `AnmolDristi/SwitchUserHelper.cs` | Static impersonation engine: authorization, session capture/hydrate/restore, employee search, audit |
| `AnmolDristi/SwitchUser.aspx` | Search UI with employee cards, Switch button, impersonation banner |
| `AnmolDristi/SwitchUser.aspx.cs` | Code-behind: search, switch, return, banner management |

### Modified Files
| File | Changes |
|------|---------|
| `AnmolDristi/Web.config` | Added `<add key="SwitchUserAuthorizedUsers" value="ADMIN,AHO445" />` |
| `AnmolDristi/Dristi.Master` | Added impersonation banner panel + "Switch User" sidebar link |
| `AnmolDristi/Dristi.Master.cs` | Added `SetupSwitchUserBannerAndMenu()` + impersonation-aware logout |
| `AnmolDristi/AnmolDristi.csproj` | Registered 4 new files |

### Unchanged Files
| File | Reason |
|------|--------|
| `login.aspx` / `login.aspx.cs` | No changes to login flow |
| `UserAuthService.cs` | Not used by impersonation |
| `SessionHelper.cs` | Not modified; heartbeat/LoginToken preserved |
| `BasePage.cs` | Session validation unchanged |
| `PermissionHelper.cs` | Permission loading unchanged |
| `Global.asax.cs` | Cleanup timer unchanged |

---

## 4. Security Properties

| Property | Guarantee |
|----------|-----------|
| No parallel auth | Reuses existing session keys; no new auth tables or tokens |
| No session fixation | Original `LoginToken`/`ActiveSessionId` preserved; no new DB session created |
| No nested impersonation | `IsImpersonating` flag checked before every switch |
| No self-switch | `WORKMAN` compared against target `EmployeeCode` |
| Fail-closed | Empty allowlist = deny all; any exception = deny |
| Audit trail | Every switch/return logged with timestamp, employee codes, session ID, IP |
| Logout integrity | `btn_Logout_Click` returns from impersonation before executing real logout |
| Permission isolation | Target user gets only their own `UserRole`→`RolePermission` chain |
