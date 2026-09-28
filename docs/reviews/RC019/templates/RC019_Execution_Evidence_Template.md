# RC-019 Execution Evidence

**Release:** RC-019  
**Status:** Template. Copy this file before a run. Leave this copy unchanged.  
**Consumers:** Script 032 and later. Scripts 031, 031b, and 031c are the SQL skeletons and are not filled in here.  
**Authority:** `docs/reviews/RC019/templates/README.md`, `Database/Scripts/031_RC019_Execution_Template.sql`, `Database/Scripts/031b_RC019_Validation_Template.sql`, `Database/Scripts/031c_RC019_Rollback_Template.sql`

One copied file records one RunTag. A later run of the same script, including a dry run that a later apply supersedes, gets a new RunTag and a new copy.

Paste banner lines, `COMPLETE` lines, and `OVERALL` lines as printed. A paraphrase is not evidence.

---

## 1. Identity

| Field | Value |
| --- | --- |
| Release | RC-019 |
| Script number | |
| Script file | `Database/Scripts/` |
| Validation script | `Database/Scripts/` |
| Rollback script | `Database/Scripts/` |
| RunTag | |
| Server | |
| Database | |
| Expected catalog | |
| Operator | |
| Start | |
| End | |
| UTC timestamp | |

Rules for these fields:

- **RunTag** is the `YYYYMMDD_HHMMSS` value printed on the apply (or dry-run) banner. It comes from local `GETDATE()` at batch start. Copy it. Do not invent one when the banner did not print, and do not reuse an earlier RunTag for a later batch.
- **Server** is `@@SERVERNAME` from that banner. **Database** is `DB_NAME()` from that banner. **Expected catalog** is the catalog named by the execution gate for this script. A mismatch is a stop: do not apply.
- **Operator** is the person who ran the batch.
- **Start** is the local time the batch was submitted. **End** is the local time the batch returned. Record both. The RunTag is the start clock of that batch, not the end.
- **UTC timestamp** is the `SYSUTCDATETIME()` value on the same banner line (`yyyy-mm-ddThh:mm:ss.fffffff`). It is not a conversion of the RunTag.

---

## 2. SHA-256 verification

Recompute on the deployment workstation immediately before the dry run. A mismatch stops the run. Do not execute a mismatched file.

```powershell
Get-FileHash -Algorithm SHA256 -Path .\Database\Scripts\<file>.sql
```

| Role | Path | Approved SHA-256 | Workstation SHA-256 | Match |
| --- | --- | --- | --- | --- |
| Apply | | | | |
| Validation | | | | |
| Rollback | | | | |

The approved hash is the hash recorded when that script was accepted. The workstation hash is the hash computed above. **Match** is `Yes` or `No`.

Session edits used to run the batch stay in the session. They are not written back into the script file. A file that no longer matches its approved hash has changed and is out of this run.

---

## 3. Pre-flight

| Check | Result |
| --- | --- |
| Full database backup, outside these scripts, is named and restorable | |
| Session catalog equals the expected catalog | |
| Apply script still defaults to `@DryRun = 1` | |
| Rollback script still defaults to `@Confirm = 0` | |
| SHA-256 match on apply, validation, and rollback | |
| Prior script in this window, if any, is OVERALL PASS | |

The full database backup does not replace the script backup table in section 6. Record its name and time:

| Full backup | Value |
| --- | --- |
| Name | |
| Time | |

---

## 4. Dry-run evidence

Run the apply script with `@DryRun` left at `1`. The dry run returns before any backup.

| Field | Value |
| --- | --- |
| RunTag | |
| UTC timestamp | |
| Start | |
| End | |
| Result | `PASS` / `STOP` / `not run` |
| Backup created | `No` |
| Intended backup name (printed, not created) | |

Paste the banner line:

```text

```

Paste the `COMPLETE` line:

```text

```

Dry-run output that matters for the later apply:

| Observation | Printed value |
| --- | --- |
| Preflight row count | |
| Discovery floor, if printed | |
| Would INSERT / would UPDATE | |
| LIVE-only rows retained | |
| Already applied, blocked, or gaps = 0 | |
| Large-copy warning | `Yes` / `No` |

A `STOP` is the result of this RunTag. Leave **Applied** as `No`. Do not set `@DryRun = 0` on this result. A later attempt is a new evidence file.

`PASS` here means the dry run completed and its printed effect matches the execution gate. It does not mean the change is applied.

---

## 5. Apply evidence

Set `@DryRun = 0` only in the session, only after section 4 is `PASS`, and only when the execution gate allows this script to write. Verification with nothing to change stays on the dry-run path and does not get `@DryRun = 0`.

| Field | Value |
| --- | --- |
| Applied | `Yes` / `No` |
| RunTag | |
| UTC timestamp | |
| Start | |
| End | |
| Session catalog | |
| Inserted | |
| Updated | |
| Rows before | |
| Rows after | |
| Indexes before | |
| Indexes after | |

Paste the banner line:

```text

```

Paste the `COMPLETE` line (`committed`, `backup skipped`, `already applied`, or `blocked`):

```text

```

When apply did not run, state the reason in one line:

```text

```

Allowed reasons include: dry-run STOP, already applied, change blocked, verification found nothing to change, template latch (`@ApplyImplemented = 0`), catalog mismatch.

---

## 6. Backup table

| Field | Value |
| --- | --- |
| Backup table | |
| Row count | |
| Created | `Yes` / `No` |
| Reason when not created | |

The created name has this shape:

`dbo.<ReleaseToken>_Backup_<ObjectToken>_<RunTag>`

Copy that name from the apply output. When no backup was created, write `not created` and the reason from section 5. Do not invent a table name.

The backup stays in place after the change transaction. Rollback reads it and does not drop it.

---

## 7. Validation summary

Run the companion validation script after the apply batch returns. When a backup was created, set `@BackupSchema` and `@BackupName` to that table. The validation batch prints its own RunTag. Record it here. The evidence file keeps the apply RunTag from section 1.

| Field | Value |
| --- | --- |
| Validation script | |
| Validation RunTag | |
| UTC timestamp | |
| Start | |
| End | |
| Mode | `READ-ONLY` |

Paste the banner line:

```text

```

Canonical checklist. Copy each printed result. `not printed` is allowed only when the template returned before the checklist, and that result is a FAIL.

| Check | Result |
| --- | --- |
| Columns Match | |
| PK Match | |
| FK Match | |
| Defaults Match | |
| Check Constraint | |
| Indexes Match | |
| Triggers Match | |
| Row Count | |
| No Duplicate Keys | |
| DBCC CHECKCONSTRAINTS | |
| MERGE Idempotent | |
| Identity preserved | |
| Count equals backup | |

---

## 8. OVERALL

Paste the footer as printed:

```text

```

| Field | Value |
| --- | --- |
| OVERALL | `PASS` / `FAIL` |
| Failed checks | |
| Next script may start | `Yes` / `No` |

`PASS` is the line `===== OVERALL: PASS =====`. Any other footer, or a validation batch that returned without that line, is `FAIL`. The next script number waits until this line is `PASS`.

---

## 9. Rollback reference

Record the companion rollback script on every package, including a package that did not change data.

| Field | Value |
| --- | --- |
| Rollback script | `Database/Scripts/` |
| Rollback executed | `No` / `Yes` |
| Confirm | `0` / `1` |
| Apply backup left intact | |
| Safety copy | |
| Rollback RunTag | |
| Rollback operator | |
| Validation after rollback | `not applicable` / `PASS` / `FAIL` |

`@Confirm` stays `0` unless this package's execution gate sets it to `1` for that session. Confirm `0` writes nothing.

A committed rollback uses the `c` script for this script number only. Its RunTag is the rollback clock, not the apply RunTag in section 1. The safety-copy name, when printed, has this shape:

`dbo.<ReleaseToken>_PreRollback_<ObjectToken>_<RollbackRunTag>`

After a committed rollback, run the validation script again and paste its OVERALL line:

```text

```

Update the copied rollback register from this section. Leave this template unchanged.

---

## 10. CAB notes

A PASS does not retire a decision that an owner has not signed.

| Decision | Default in force | Signed change | Notes |
| --- | --- | --- | --- |
| | | `No` / `Yes` | |

Unsigned RC-018 holds stay on `RC018_Execution_Closeout_20260927.md`. Copy one into this table only when this script's execution gate names it.

---

## 11. Close

| Item | Value |
| --- | --- |
| Evidence file | `docs/reviews/RC019/executions/` |
| Matrix row updated | `Yes` / `No` |
| Rollback register row updated | `Yes` / `No` |
| Raw output | `docs/reviews/RC019/evidence/` |
