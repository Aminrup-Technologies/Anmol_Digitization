# RC-019 Rollback Register

**Release:** RC-019  
**Status:** Template. Copy this file once per execution window. Leave this copy unchanged.  
**Consumers:** Script 032 and later.  
**Authority:** `docs/reviews/RC019/templates/README.md`, `Database/Scripts/031c_RC019_Rollback_Template.sql`

The copy is the inventory of script backup tables for the window, and the record of any rollback that later uses one. Add a row when the apply batch returns, including a run that created no backup.

---

## Window

| Field | Value |
| --- | --- |
| Release | RC-019 |
| Window | |
| Server | |
| Database | |
| Register file | `docs/reviews/RC019/executions/` |

---

## Register

| Backup table | Script | RunTag | Rollback executed | Validation after rollback | Operator |
| --- | --- | --- | --- | --- | --- |
| | | | | | |

### Allowed values

| Column | Values |
| --- | --- |
| Backup table | Printed name `dbo.<ReleaseToken>_Backup_<ObjectToken>_<RunTag>`, or `none (<reason>)`. |
| Script | Apply script number, for example `032`. The rollback file is `<script>c`. |
| RunTag | Apply RunTag from the evidence file. |
| Rollback executed | `No` or `Yes`. |
| Validation after rollback | `not applicable`, `PASS`, or `FAIL`. |
| Operator | Person accountable for the backup. After a rollback commits, the person who ran the `c` script. |

### How a row moves

1. At apply time, set **Rollback executed** to `No` and **Validation after rollback** to `not applicable`.
2. **Operator** is the apply operator until a rollback runs.
3. A rollback runs only from the matching `c` script, and only after that script's execution gate sets `@Confirm = 1` in the session. `@Confirm = 0` writes nothing and leaves this row unchanged.
4. The `c` script names the backup explicitly. It does not rebuild the name from the clock. A missing backup stops the rollback before any write.
5. After a committed rollback, set **Rollback executed** to `Yes`. The apply backup stays in place. Record that fact on the evidence file, together with any safety copy `dbo.<ReleaseToken>_PreRollback_<ObjectToken>_<RollbackRunTag>`.
6. Run the companion validation script again. Set **Validation after rollback** from its `OVERALL` line. `PASS` is `===== OVERALL: PASS =====`.
7. The rollback batch has its own RunTag. Keep the apply RunTag in this table. Record the rollback RunTag on the evidence file.

`none (<reason>)` uses the same reasons as the execution matrix: `dry run`, `already applied`, `blocked`, `verify found nothing to change`, or `template latch`. That cell is the record that no backup object exists. Do not invent a table name.

A PASS on the execution matrix does not delete a row here. The backup remains available for the life of the window.
