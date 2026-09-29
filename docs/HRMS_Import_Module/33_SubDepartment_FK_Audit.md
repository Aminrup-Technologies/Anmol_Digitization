# 33 — SubDepartment FK Resolution Audit (False Warning)

**Status:** Analysis only — **no SQL code changes in this deliverable.**  
**Scope:** Audit of `usp_HSE_InsertNewEmployees` and `usp_HSE_UpdateChangedEmployees` SubDepartment FK resolution.  
**UAT symptom:** Staging shows  
`[WARNING] SubDepartment_ID '1559' not found in MST_SubDepartment; expected to be created by SynchronizeMasters.`  
while `MST_SubDepartment` already has `Id = 122`, `SubDepartment_ID = 1559`, `SubDepartment_Description = FSMS Coordinator`.

---

## 1. Executive verdict

| Question | Finding |
|---|---|
| Do Insert / Update join on the wrong column (`Id` vs `SubDepartment_ID`)? | **No** — both match **business code** `SubDepartment_ID`, then store **`Id`**. |
| Does Insert / Update emit the quoted warning text? | **No** — that exact string exists only in `usp_HSE_ValidateBatch`. |
| Is `MST_UserMaster.SubDepartmentId` written with `MST_SubDepartment.Id`? | **Yes** (when the join resolves). |
| Is there a confirmed Insert/Update FK defect for SubDepartment? | **No defect confirmed** in current repository SQL. |
| Likely explanation of the UAT “false” warning | **Retained pre-sync validation warning** (or a ValidateBatch lookup miss), not an Insert/Update join bug. |

---

## 2. Warning attribution (critical)

Exact UAT text:

```text
[WARNING] SubDepartment_ID '1559' not found in MST_SubDepartment; expected to be created by SynchronizeMasters.
```

| Procedure | Produces this text? | SubDepartment behaviour |
|---|---|---|
| `usp_HSE_ValidateBatch` | **Yes** (only source) | `NOT EXISTS` on master business code → Warning if description present, Error if empty |
| `usp_HSE_InsertNewEmployees` | **No** | Optional FK; unresolved → `NULL` SubDepartmentId; **no SubDepartment warning** |
| `usp_HSE_UpdateChangedEmployees` | **No** (different text) | Soft warning only if join fails: `[WARNING] Unresolved master reference(s) during update: SubDepartment_ID '…'. Unresolved FK fields were left unchanged.` |

ValidateBatch source (for attribution only — out of Insert/Update fix scope unless product chooses to change validation messaging):

```sql
-- usp_HSE_ValidateBatch.sql
ELSE N'[WARNING] SubDepartment_ID ''' + LTRIM(RTRIM(s.SubDepartment_ID))
     + N''' not found in MST_SubDepartment; expected to be created by SynchronizeMasters.'
...
AND NOT EXISTS (
      SELECT 1
      FROM dbo.MST_SubDepartment AS m WITH (NOLOCK)
      WHERE CAST(m.SubDepartment_ID AS NVARCHAR(50)) = LTRIM(RTRIM(s.SubDepartment_ID))
    );
```

---

## 3. Resolution chain (required)

```text
Staging_EmployeeData.SubDepartment_ID     (HRMS business code, e.g. 1559)
        ↓ match
MST_SubDepartment.SubDepartment_ID
        ↓ take PK
MST_SubDepartment.Id                      (e.g. 122)
        ↓ store
MST_UserMaster.SubDepartmentId
```

UAT data matches this model: business code `1559` ↔ identity `122`.

---

## 4. Audit — `usp_HSE_InsertNewEmployees`

### 4.1 Join condition — Current SQL

```sql
SubDepartmentCode = NULLIF(LTRIM(RTRIM(s.SubDepartment_ID)), N''),
...
SubDepartmentId = sd.Id,
...
LEFT JOIN dbo.MST_SubDepartment AS sd
    ON CAST(sd.SubDepartment_ID AS NVARCHAR(50)) = r.SubDepartmentCode
```

Inserted column:

```sql
INSERT INTO dbo.MST_UserMaster ( … SubDepartmentId … )
SELECT … c.SubDepartmentId …
```

### 4.2 Assessment

| Check | Result |
|---|---|
| Matches business code `SubDepartment_ID`? | **Yes** (`CAST` to `NVARCHAR(50)` vs staging code) |
| Stores identity `Id`? | **Yes** (`sd.Id` → `MST_UserMaster.SubDepartmentId`) |
| Tests `Id` by mistake? | **No** |
| Warns when SubDepartment missing? | **No** — optional FK; only Branch/Plant are hard required |

### 4.3 Correct SQL

**Same as current** for SubDepartment FK resolution. No Insert change proposed.

---

## 5. Audit — `usp_HSE_UpdateChangedEmployees`

### 5.1 Join condition — Current SQL

```sql
SubDepartmentCode = NULLIF(LTRIM(RTRIM(s.SubDepartment_ID)), N''),
...
NewSubDepartmentId = CASE
    WHEN @UpdSubDepartment = 1 AND r.SubDepartmentCode IS NOT NULL
    THEN sd.Id ELSE NULL END,
...
MissingMaster = CASE
    …
    OR (@UpdSubDepartment = 1 AND r.SubDepartmentCode IS NOT NULL AND sd.Id IS NULL)
    THEN 1 ELSE 0 END,
...
CASE WHEN @UpdSubDepartment = 1 AND r.SubDepartmentCode IS NOT NULL AND sd.Id IS NULL
    THEN N'; SubDepartment_ID ''' + r.SubDepartmentCode + N'''' ELSE N'' END
...
LEFT JOIN dbo.MST_SubDepartment AS sd
    ON CAST(sd.SubDepartment_ID AS NVARCHAR(50)) = r.SubDepartmentCode
```

Applied update:

```sql
u.SubDepartmentId = CASE
    WHEN c.NewSubDepartmentId IS NOT NULL
     AND ISNULL(u.SubDepartmentId, -1) <> c.NewSubDepartmentId
    THEN c.NewSubDepartmentId ELSE u.SubDepartmentId END
```

### 5.2 Assessment

| Check | Result |
|---|---|
| Matches business code? | **Yes** (same `CAST` join as Insert / SynchronizeMasters / ValidateBatch) |
| Stores identity `Id`? | **Yes** (`NewSubDepartmentId = sd.Id`) |
| Warning when master exists? | Only if `sd.Id IS NULL` after that join — **should not fire** for `SubDepartment_ID = 1559` when row `Id = 122` exists |
| Warning text vs UAT? | **Different** — Update does not say “expected to be created by SynchronizeMasters” |

### 5.3 Correct SQL

**Same as current** for SubDepartment FK resolution. No Update change proposed for the quoted UAT message.

---

## 6. Why the warning can appear while the master exists (root cause)

### 6.1 Primary root cause (most consistent with pipeline design)

```text
ValidateBatch          ← writes [WARNING] if master not found YET
        ↓
SynchronizeMasters     ← creates/updates MST_SubDepartment (1559 → Id 122)
        ↓
Insert / Update        ← resolve CAST(SubDepartment_ID) → Id 122 (correct)
        ↓
Orchestrator finalize  ← row → Processed; ErrorMessage WARNING text retained
```

| Moment | Master 1559 | Warning text |
|---|---|---|
| During `usp_HSE_ValidateBatch` | Often **absent** (first import) | Written — **true at that time** |
| After `usp_HSE_SynchronizeMasters` | **Present** (`Id = 122`) | Still on staging row |
| After Insert/Update | Present; FK resolved | Still on staging row (by design — Phase 4.1 keeps `[WARNING]` text) |
| Post-UAT DB check | Present | Looks like a **false** warning |

So the message is typically a **stale pre-sync validation warning**, not proof that Insert/Update failed to find SubDepartment.

ValidateBatch **resets** `ErrorMessage` at the start of each validation run, then re-applies checks. A warning that survives a full Run Import is therefore from **that run’s** validation step (before masters), not from a previous batch run.

### 6.2 Secondary hypothesis (only if master already existed before validation)

If `MST_SubDepartment` already contained `SubDepartment_ID = 1559` **before** ValidateBatch, the `NOT EXISTS` should have been false and **no** warning should have been written. That would imply a lookup miss (type/whitespace/invisible chars), not an Id-vs-code column swap — current SQL already compares business codes.

Insert/Update use the **same** business-code predicate as ValidateBatch and SynchronizeMasters. If SynchronizeMasters matched/updated that row, Insert/Update should match it too when they run afterward.

### 6.3 What was ruled out for Insert/Update

| Hypothesis | Ruled out? |
|---|---|
| Join tests `MST_SubDepartment.Id = 1559` (wrong column) | **Yes** — join uses `SubDepartment_ID` |
| Warning generated inside Insert | **Yes** — Insert has no such warning |
| Warning generated inside Update with the UAT wording | **Yes** — wording is ValidateBatch-only |
| `MST_UserMaster.SubDepartmentId` stores business code `1559` from HSE Insert/Update | **Yes** — HSE stores `sd.Id` |

---

## 7. Current SQL vs Correct SQL (summary)

| Area | Current SQL | Correct SQL | Change needed? |
|---|---|---|---|
| Insert join | `CAST(sd.SubDepartment_ID AS NVARCHAR(50)) = staging code` → `sd.Id` | Same | **No** |
| Update join | Same | Same | **No** |
| UserMaster store | `SubDepartmentId = sd.Id` | Same | **No** |
| Quoted warning | Emitted by **ValidateBatch** before masters sync; retained after | N/A to Insert/Update | See §8 |

---

## 8. Proposed smallest fix (if product wants to remove the “false” look)

**Do not change Insert/Update SubDepartment joins** — they are already correct.

| Option | Where | Size | Effect |
|---|---|---|---|
| **A (recommended, smallest behavioural fix)** | Orchestrator **or** end of `SynchronizeMasters` / post-master step | Small | After masters sync, clear or rewrite staging `[WARNING] … expected to be created by SynchronizeMasters` fragments for codes that now exist in `MST_SubDepartment` |
| **B (messaging only)** | `usp_HSE_ValidateBatch` | Tiny | Soften wording to “not found at validation time; SynchronizeMasters will create if Description is present” so post-run reads don’t look like a failed lookup |
| **C (no code)** | Ops / docs | None | Document that this WARNING is pre-sync and may remain after a successful master create |

**Out of scope for this audit’s code change mandate:** implementing A/B (user requested analysis first; Insert/Update have no confirmed defect).

If a future hotfix is approved for Option A, constrain it to SubDepartment (or all “expected to be created” master warnings) and do not alter FK resolution or sync business rules.

---

## 9. UAT verification queries (read-only)

```sql
-- Staging vs master for a batch
SELECT s.StagingId, s.EmpCode,
       s.SubDepartment_ID, s.SubDepartment_Description,
       s.ImportStatus, s.ErrorMessage
FROM dbo.Staging_EmployeeData AS s
WHERE s.BatchId = @BatchId
  AND LTRIM(RTRIM(s.SubDepartment_ID)) = N'1559';

SELECT Id, SubDepartment_ID, SubDepartment_Description
FROM dbo.MST_SubDepartment
WHERE CAST(SubDepartment_ID AS NVARCHAR(50)) = N'1559';

-- Confirm UserMaster stored identity Id (122), not business code (1559)
SELECT u.EmployeeCode, u.SubDepartmentId, sd.SubDepartment_ID, sd.Id
FROM dbo.MST_UserMaster AS u
LEFT JOIN dbo.MST_SubDepartment AS sd ON sd.Id = u.SubDepartmentId
WHERE u.EmployeeCode = @EmpCode;
```

Expected after a good sync: `u.SubDepartmentId = 122`, `sd.SubDepartment_ID = 1559`.

---

## 10. Files reviewed

| File | Role in finding |
|---|---|
| `Database/Stored Procedures/HRMS/usp_HSE_InsertNewEmployees.sql` | FK join + insert of `SubDepartmentId` — **correct** |
| `Database/Stored Procedures/HRMS/usp_HSE_UpdateChangedEmployees.sql` | FK join + update + different warning text — **correct join** |
| `Database/Stored Procedures/HRMS/usp_HSE_ValidateBatch.sql` | **Sole source** of the quoted WARNING text |
| `Database/Stored Procedures/HRMS/usp_HSE_SynchronizeMasters.sql` | Creates/updates by same business-code match (context) |
| `Database/Tables/MST_UserMaster.sql` | `SubDepartmentId int NULL` |

**Files that would be affected by a future Option A/B fix (not applied here):**  
`usp_HSE_ValidateBatch.sql` and/or `usp_RunHRMSSynchronization.sql` (or a tiny post-master cleanup helper) — **not** Insert/Update FK logic.

---

## 11. Reason (one paragraph)

Insert and Update already resolve SubDepartment as business code → `MST_SubDepartment.Id` and do not emit the UAT warning string. That string is written by ValidateBatch when the master is missing at validation time (with description present so SynchronizeMasters can create it). After masters sync, the row becomes Processed but the warning text is retained, so a post-run lookup of `MST_SubDepartment` makes a historically correct warning look false. No Insert/Update SQL change is required for this incident.
