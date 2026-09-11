# BUILD_EVIDENCE.md — VS2015 Build Validation

**Date:** ___________  
**Machine:** ___________  
**VS Version:** Visual Studio 2015  

## Steps

1. Open `AnmolDristi.sln` in VS2015
2. Build → Clean Solution
3. Build → Rebuild Solution

## Expected Result

```
========== Clean: 1 succeeded, 0 failed, 0 skipped ==========
========== Rebuild: 1 succeeded, 0 failed, 0 skipped ==========
```

## Compile Items Check

Verify no duplicate `<Compile>` entries in `AnmolDristi.csproj`:

```xml
<!-- New files registered -->
<Compile Include="SwitchUserHelper.cs" />
<Compile Include="SwitchUser.aspx.cs">
  <DependentUpon>SwitchUser.aspx</DependentUpon>
  <SubType>ASPXCodeBehind</SubType>
</Compile>
<Compile Include="SwitchUser.aspx.designer.cs">
  <DependentUpon>SwitchUser.aspx</DependentUpon>
</Compile>
```

## Verification

- [ ] Clean: 1 succeeded, 0 failed
- [ ] Rebuild: 1 succeeded, 0 failed
- [ ] No duplicate Compile items
- [ ] No warnings related to SwitchUser files

## Result

**PASS / FAIL:** ___________

**Notes:** ___________
