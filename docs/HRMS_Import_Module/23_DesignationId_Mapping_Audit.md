# 23 — Designation_ID Mapping Audit (Production Readiness)

**Mode:** Analysis only. **No SQL, C#, or migration changes were made.**  
**Date:** 2026-08-04  
**Scope:** Verify that `MST_UserMaster.Designation_ID` always stores `MST_Designation.Id` (ERP identity), never the HRMS business code `MST_Designation.Designation_ID`.

---

## 0. Business rule under audit

```text
HRMS / Staging.Designation_ID     (business code, e.g. 90)
        ↓ match
MST_Designation.Designation_ID    (varchar business code = '90')
        ↓ take PK
MST_Designation.Id                (ERP identity, e.g. 133)
        ↓ store
MST_UserMaster.Designation_ID     (int)  ← MUST be 133, NOT 90
```

Evidence that the application expects **Id**:

| Consumer | Join / usage |
|---|---|
| `viewusers.aspx.cs` | `u.Designation_ID = desg.Id` |
| `PolicyExceptionDataAccess.cs` | `u.Designation_ID = d.Id` |
| `usp_ResolveDesignation` | Resolves input as Id **or** business code → returns `Id` |

---

## 1. Insert procedure review — `usp_HSE_InsertNewEmployees`

**Verdict: CORRECT (permanent fix already present).**

| Step | Code | Result |
|---|---|---|
| Staging code | `DesignationCode = LTRIM(RTRIM(s.Designation_ID))` | Business code |
| Resolve | `LEFT JOIN MST_Designation des ON des.Designation_ID = r.DesignationCode` | Match on business key |
| Capture PK | `DesignationId = des.Id` | ERP identity |
| Write | `INSERT … Designation_ID … SELECT … c.DesignationId` | Stores **Id** |

Does **not** write the HRMS business code directly into `MST_UserMaster.Designation_ID`.

Header comment in the procedure explicitly documents:

```text
Designation_ID  ← MST_Designation.Id   (NOT Designation_ID business code)
```

---

## 2. Update procedure review — `usp_HSE_UpdateChangedEmployees`

**Verdict: CORRECT (permanent fix already present).**

| Step | Code | Result |
|---|---|---|
| Staging code | `DesignationCode = … s.Designation_ID` | Business code |
| Resolve | `LEFT JOIN MST_Designation des ON des.Designation_ID = r.DesignationCode` | Match on business key |
| Proposed value | `NewDesignationId = … des.Id` | ERP identity |
| Write | `u.Designation_ID = … c.NewDesignationId` | Stores **Id** |

Change-detection compares against `NewDesignationId` (Id), not the staging business code.

---

## 3–4. Repository audit — every assignment to `MST_UserMaster.Designation_ID`

Search covered: `INSERT` / `UPDATE` / `MERGE` / dynamic SQL / C# `AddWithValue` against `MST_UserMaster.Designation_ID` across `Database/` and `AnmolDristi/` (excluding packages/bin/obj).

### Writers (assignments)

| # | File | Procedure / method | Line(s) | Current assignment | Correct? |
|---|---|---|---|---|---|
| 1 | `Database/Stored Procedures/HRMS/usp_HSE_InsertNewEmployees.sql` | `usp_HSE_InsertNewEmployees` | 158, 193, 267, 292 | `DesignationId = des.Id` → `INSERT … Designation_ID … c.DesignationId` | **YES** |
| 2 | `Database/Stored Procedures/HRMS/usp_HSE_UpdateChangedEmployees.sql` | `usp_HSE_UpdateChangedEmployees` | 243, 301, 385–388 | `NewDesignationId = des.Id` → `u.Designation_ID = c.NewDesignationId` | **YES** |

### Non-writers (related, but do **not** assign `MST_UserMaster.Designation_ID`)

| File | Procedure / method | Notes |
|---|---|---|
| `usp_HSE_SynchronizeMasters.sql` | Designation master upsert | Writes `MST_Designation.Designation_ID` / Description only — correct for master table |
| `usp_HSE_ValidateBatch.sql` | Validates staging vs `MST_Designation.Designation_ID` | Read-only on masters |
| `usp_HSE_UpdateReportingHierarchy.sql` | Hierarchy only | Does not touch Designation |
| `usp_RunHRMSSynchronization.sql` | Orchestrator | No Designation DML |
| `New_User.aspx.cs` | `BtnSubmit_Click` INSERT | Inserts `MST_UserMaster` **without** `Designation_ID` column |
| `EditEmployee.aspx.cs` | UPDATE | Updates many org fields; **does not** set `Designation_ID` |
| `UserAuthService.cs` | INSERT | Password bootstrap only; no Designation |
| `004_PolicyException_StoredProcedures.sql` | Exception insert | **Reads** `UM.Designation_ID` into policy exception table; does not update UserMaster |

### MERGE

No `MERGE` into `MST_UserMaster` found in the repository.

### Dynamic SQL

No dynamic SQL assigning `MST_UserMaster.Designation_ID` found.

---

## 5. Incorrect assignments — remediation sketch (not applied)

**No incorrect assignment exists in the current HSE write path.**

If a hypothetical incorrect assignment were:

```sql
-- OLD (incorrect): write HRMS business code
u.Designation_ID = TRY_CONVERT(INT, s.Designation_ID)
-- or
c.DesignationId = TRY_CONVERT(INT, r.DesignationCode)
```

Correct form (already implemented):

```sql
-- CORRECT
LEFT JOIN dbo.MST_Designation AS des
    ON des.Designation_ID = r.DesignationCode
…
-- store des.Id
u.Designation_ID = des.Id
```

**No automatic code change required for Insert/Update procedures.**

---

## 6. Existing data assessment — is a one-time data fix still required?

| Question | Answer |
|---|---|
| Do current HSE Insert/Update procedures permanently enforce Id mapping? | **Yes** |
| Can this repository prove live `MST_UserMaster` data is already clean? | **No** — no production data access in this audit |
| Could historical rows still hold HRMS business codes? | **Yes, possible** — legacy `usp_RunHRMSSynchronization` / earlier batch processors are **not** in source; prior behaviour is unknown |
| Other app writers of `Designation_ID`? | **None found** in current repo (New User / Edit Employee omit the column) |

**Conclusion:**  
- **Permanent procedural fix: already in place** for HSE Insert + Update.  
- **One-time data remediation: still recommended to assess on production** using §7 SQL. If counts in set A are non-zero, a carefully guarded data fix is warranted. If A = 0 and B covers all non-null designations, no data fix is required.

---

## 7. Verification SQL (run on target database)

### A. Employees still storing HRMS business code  
(value matches `MST_Designation.Designation_ID` but does **not** equal that row’s `Id`, and does not match any designation as `Id`)

```sql
-- A: Likely HRMS business codes stored in UserMaster.Designation_ID
SELECT
    u.EmployeeCode,
    u.EmployeeName,
    u.Designation_ID AS StoredValue,
    d.Id AS CorrectErpId,
    d.Designation_ID AS BusinessCode,
    d.Designation_Description
FROM dbo.MST_UserMaster AS u
INNER JOIN dbo.MST_Designation AS d
    ON d.Designation_ID = CAST(u.Designation_ID AS VARCHAR(50))
WHERE u.Designation_ID IS NOT NULL
  AND u.Designation_ID <> d.Id
  AND NOT EXISTS (
        SELECT 1
        FROM dbo.MST_Designation AS x
        WHERE x.Id = u.Designation_ID
      );
```

### B. Employees already storing ERP identity

```sql
-- B: Stored value is a valid MST_Designation.Id
SELECT
    u.EmployeeCode,
    u.EmployeeName,
    u.Designation_ID AS StoredErpId,
    d.Designation_ID AS BusinessCode,
    d.Designation_Description
FROM dbo.MST_UserMaster AS u
INNER JOIN dbo.MST_Designation AS d
    ON d.Id = u.Designation_ID
WHERE u.Designation_ID IS NOT NULL;
```

### C. Mixed / ambiguous population

```sql
-- C1: Summary classification
SELECT
    Classification =
        CASE
            WHEN u.Designation_ID IS NULL THEN N'NULL'
            WHEN EXISTS (SELECT 1 FROM dbo.MST_Designation x WHERE x.Id = u.Designation_ID)
                 AND EXISTS (
                        SELECT 1 FROM dbo.MST_Designation y
                        WHERE y.Designation_ID = CAST(u.Designation_ID AS VARCHAR(50))
                          AND y.Id <> u.Designation_ID
                     )
                THEN N'AMBIGUOUS (value is both a valid Id and another row''s business code)'
            WHEN EXISTS (SELECT 1 FROM dbo.MST_Designation x WHERE x.Id = u.Designation_ID)
                THEN N'ERP_IDENTITY'
            WHEN EXISTS (
                    SELECT 1 FROM dbo.MST_Designation y
                    WHERE y.Designation_ID = CAST(u.Designation_ID AS VARCHAR(50))
                 )
                THEN N'HRMS_BUSINESS_CODE'
            ELSE N'ORPHAN (matches neither Id nor business code)'
        END,
    COUNT(*) AS RowCount
FROM dbo.MST_UserMaster AS u
GROUP BY
        CASE
            WHEN u.Designation_ID IS NULL THEN N'NULL'
            WHEN EXISTS (SELECT 1 FROM dbo.MST_Designation x WHERE x.Id = u.Designation_ID)
                 AND EXISTS (
                        SELECT 1 FROM dbo.MST_Designation y
                        WHERE y.Designation_ID = CAST(u.Designation_ID AS VARCHAR(50))
                          AND y.Id <> u.Designation_ID
                     )
                THEN N'AMBIGUOUS (value is both a valid Id and another row''s business code)'
            WHEN EXISTS (SELECT 1 FROM dbo.MST_Designation x WHERE x.Id = u.Designation_ID)
                THEN N'ERP_IDENTITY'
            WHEN EXISTS (
                    SELECT 1 FROM dbo.MST_Designation y
                    WHERE y.Designation_ID = CAST(u.Designation_ID AS VARCHAR(50))
                 )
                THEN N'HRMS_BUSINESS_CODE'
            ELSE N'ORPHAN (matches neither Id nor business code)'
        END
ORDER BY RowCount DESC;

-- C2: Detail rows that are AMBIGUOUS (require manual review)
SELECT
    u.EmployeeCode,
    u.Designation_ID AS StoredValue,
    idMatch.Id AS MatchesAsId_Of,
    idMatch.Designation_ID AS IdRow_BusinessCode,
    codeMatch.Id AS MatchesAsCode_MapsToId,
    codeMatch.Designation_ID AS CodeRow_BusinessCode
FROM dbo.MST_UserMaster AS u
LEFT JOIN dbo.MST_Designation AS idMatch
    ON idMatch.Id = u.Designation_ID
LEFT JOIN dbo.MST_Designation AS codeMatch
    ON codeMatch.Designation_ID = CAST(u.Designation_ID AS VARCHAR(50))
   AND codeMatch.Id <> u.Designation_ID
WHERE u.Designation_ID IS NOT NULL
  AND idMatch.Id IS NOT NULL
  AND codeMatch.Id IS NOT NULL;
```

### D. Total counts

```sql
SELECT
    TotalEmployees = COUNT(*),
    NonNullDesignation = SUM(CASE WHEN Designation_ID IS NOT NULL THEN 1 ELSE 0 END),
    NullDesignation = SUM(CASE WHEN Designation_ID IS NULL THEN 1 ELSE 0 END),
    ErpIdentityCount = SUM(CASE
        WHEN EXISTS (SELECT 1 FROM dbo.MST_Designation x WHERE x.Id = u.Designation_ID)
        THEN 1 ELSE 0 END),
    HrmsBusinessCodeOnlyCount = SUM(CASE
        WHEN u.Designation_ID IS NOT NULL
         AND NOT EXISTS (SELECT 1 FROM dbo.MST_Designation x WHERE x.Id = u.Designation_ID)
         AND EXISTS (
                SELECT 1 FROM dbo.MST_Designation y
                WHERE y.Designation_ID = CAST(u.Designation_ID AS VARCHAR(50))
             )
        THEN 1 ELSE 0 END),
    OrphanCount = SUM(CASE
        WHEN u.Designation_ID IS NOT NULL
         AND NOT EXISTS (SELECT 1 FROM dbo.MST_Designation x WHERE x.Id = u.Designation_ID)
         AND NOT EXISTS (
                SELECT 1 FROM dbo.MST_Designation y
                WHERE y.Designation_ID = CAST(u.Designation_ID AS VARCHAR(50))
             )
        THEN 1 ELSE 0 END)
FROM dbo.MST_UserMaster AS u;
```

---

## 8. Proposed one-time UPDATE — safety verdict

Script under review:

```sql
UPDATE U
SET U.Designation_ID = D.Id
FROM dbo.MST_UserMaster U
INNER JOIN dbo.MST_Designation D
    ON D.Designation_ID = CAST(U.Designation_ID AS VARCHAR(50))
WHERE U.Designation_ID <> D.Id;
```

### Verdict: **NOT SAFE**

| Risk | Why |
|---|---|
| **Id ↔ business-code collision** | `MST_UserMaster.Designation_ID` is `int`; `MST_Designation.Designation_ID` is `varchar`. Numeric Ids and numeric-looking business codes share one value space. A correctly stored **Id** (e.g. `90`) can also equal another designation’s **business code** `'90'`. This UPDATE would remapped that employee to the wrong `D.Id`. |
| **Ambiguous rows** | Rows classified AMBIGUOUS in §7.C cannot be auto-fixed by this join alone. |
| **Duplicate business codes** | Multiple `MST_Designation` rows with the same `Designation_ID` make the join non-deterministic. |
| **No provenance filter** | Touches all matching UserMaster rows, including manually maintained ones. |

### Safer direction (conceptual only — not authored as a migration here)

Only update rows that are **HRMS_BUSINESS_CODE only** (not a valid Id), after reviewing AMBIGUOUS counts:

```sql
-- CONCEPT ONLY — do not run without DBA review / backup / dry-run counts
UPDATE U
SET U.Designation_ID = D.Id
FROM dbo.MST_UserMaster AS U
INNER JOIN dbo.MST_Designation AS D
    ON D.Designation_ID = CAST(U.Designation_ID AS VARCHAR(50))
WHERE U.Designation_ID IS NOT NULL
  AND U.Designation_ID <> D.Id
  AND NOT EXISTS (
        SELECT 1 FROM dbo.MST_Designation AS X WHERE X.Id = U.Designation_ID
      );
```

Even this remains **unsafe if business codes collide across duplicate `MST_Designation.Designation_ID` values**. Require:

1. Dry-run `SELECT` of affected rows  
2. Unique business codes (or `TOP 1` rule with explicit ORDER BY)  
3. Zero (or manually resolved) AMBIGUOUS rows  
4. Backup / transaction / rowcount validation  

---

## 9. Recommendations

| Item | Recommendation |
|---|---|
| HSE Insert | **No change** — already stores `MST_Designation.Id` |
| HSE Update | **No change** — already stores `MST_Designation.Id` |
| Orchestrator / other HSE children | **No Designation write path** — N/A |
| Permanent code fix | **Already exists** in Phase 3.6 / 3.7 procedures |
| Production data | Run §7 verification SQL on live DB |
| One-time correction | **Only if** `HrmsBusinessCodeOnlyCount > 0` (and after resolving AMBIGUOUS); **do not** use the unguarded UPDATE from §8 |
| Unguarded UPDATE in §8 | **NOT SAFE — do not run as-is** |

---

## 10. Final determination

| Question | Answer |
|---|---|
| Does Insert resolve Staging → business code → Id → UserMaster? | **Yes** |
| Does Update do the same? | **Yes** |
| Any incorrect repo assignment to `MST_UserMaster.Designation_ID`? | **No** |
| Is additional HSE code work required for this rule? | **No** |
| Is a one-time data fix still potentially required? | **Maybe — data-dependent; verify with §7 on production** |
| Is the sample UPDATE safe? | **NOT SAFE** |

**Bottom line:** The permanent synchronization fix is already implemented. Remaining risk is **historical data quality**, which must be measured on the target database before any remediation UPDATE is designed or executed.
