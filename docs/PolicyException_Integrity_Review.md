# Policy Exception Integrity Review (Phase 1)

Scope: database integrity only. No UI, policy resolution, or claim-processing changes.

## Reviewed procedures

| Procedure | Current validations | Gaps |
|-----------|---------------------|------|
| `usp_InsertPolicyException` | Employee required/exists/active; PolicyJson non-empty; Priority > 0; EffectiveFrom required; EffectiveTo ≥ From; active overlap | No `ISJSON`; overlap check not concurrency-safe (empty-set race); timestamps use `SYSDATETIME()` |
| `usp_UpdatePolicyException` | Same as insert + ExceptionId exists; overlap excludes self | No `ISJSON`; no row lock before update; empty-set/overlap race; `SYSDATETIME()` |
| `usp_SetPolicyExceptionActiveStatus` | Exists; not already in requested status | No expired-date check on Activate; no overlap check on Activate; no concurrency lock; history written after update (safe only if checks precede mutate) |
| `usp_SearchPolicyExceptions` | Optional filters | OK for integrity |
| `usp_GetPolicyExceptionById` | By id | OK |
| `usp_InsertPolicyExceptionHistorySnapshot` | ActionType check; `UPDLOCK, HOLDLOCK` for VersionNo | OK; history writes IST |

## Findings summary

1. **JSON** – malformed JSON can be stored (`ISJSON` not used).
2. **Activation** – expired rows (`EffectiveTo < today`) can be activated; activating can create overlapping actives.
3. **Concurrency** – Insert/Update/Activate race on overlap checks when no conflicting row exists yet (`UPDLOCK` alone insufficient).
4. **Indexes** – `PolicyException` has PK only; resolution/overlap/search by employee scan.
5. **Data quality** – no table CHECKs for Priority / date order; EmployeeCode validated in SP only (no FK; intentional — UserMaster key types differ).
6. **Audit timestamps** – `CreatedOn`/`ModifiedOn` = server `SYSDATETIME()`; history `ChangedOn` = IST. **Inconsistency.**
7. **History security** – no UPDATE/DELETE procedures, but table not protected against direct DML.

## Decisions for this hardening PR

| Topic | Decision |
|-------|----------|
| Concurrency | Transaction-scoped `sp_getapplock` on `PolicyException:{EmployeeCode}` + `UPDLOCK, HOLDLOCK` on target row for Update/Activate |
| Why applock | Least intrusive fix for empty-set race without SERIALIZABLE; employee-scoped; auto-released on commit/rollback |
| Timestamps | Align write-path `CreatedOn`/`ModifiedOn` to IST to match `ChangedOn` |
| Indexes | Add one covering index for employee active/date lookups |
| History | AFTER trigger rejects UPDATE/DELETE |
