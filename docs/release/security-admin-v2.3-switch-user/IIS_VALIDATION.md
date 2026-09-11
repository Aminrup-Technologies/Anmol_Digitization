# IIS_VALIDATION.md — IIS Deployment & Login Validation

**Date:** ___________  
**IIS Site:** AnmolDristi  
**URL:** http://localhost/AnmolDristi/  

## Steps

1. Copy build output to IIS application folder
2. Recycle application pool
3. Navigate to `http://localhost/AnmolDristi/login.aspx`

## Login Page Verification

- [ ] Login page loads without errors
- [ ] Company logo visible
- [ ] Employee Code field present
- [ ] Password field present
- [ ] Login button functional

## Normal Login Test

1. Enter valid employee code (e.g., `S319`)
2. Enter valid password
3. Click Login
4. Verify redirect to `home.aspx`

## Post-Login Session Verification

After login, check browser sessionStorage:
```javascript
console.log(sessionStorage.getItem('ActiveLoginToken'));
console.log(sessionStorage.getItem('ActiveSessionId'));
```

- [ ] `ActiveLoginToken` is a valid GUID
- [ ] `ActiveSessionId` is a valid integer

## Sidebar Verification

- [ ] User name displayed in profile section
- [ ] Menu items visible based on permissions
- [ ] "Switch User" link: **NOT visible** (for non-allowlisted users)

## Result

**PASS / FAIL:** ___________

**Notes:** ___________
