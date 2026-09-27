# RC-019 Execution Matrix

**Release:** RC-019  
**Status:** Template. Copy this file once per execution window. Leave this copy unchanged.  
**Consumers:** Script 032 and later.  
**Authority:** `docs/reviews/RC019/templates/README.md`

One row is one RunTag. Validation and rollback for that run sit on the same row. A dry run that stops, and a later apply of the same script, are two rows.

Add rows to the copy as each batch returns. Do not start the next script number until the current row's **Validation** is `PASS`.

---

## Window

| Field | Value |
| --- | --- |
| Release | RC-019 |
| Window | |
| Server | |
| Database | |
| Expected catalog | |
| Operator | |
| Full database backup (outside the scripts) | |
| Matrix file | `docs/reviews/RC019/executions/` |

---

## Matrix

| Script | RunTag | DryRun | Applied | Validation | Backup | Rollback | Result | Evidence File |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | | | | | | | | |

### Allowed values

| Column | Values |
| --- | --- |
| Script | Script number of the apply file, for example `032`. |
| RunTag | `YYYYMMDD_HHMMSS` from that batch's banner. Copy it. Do not invent it. |
| DryRun | `PASS`, `STOP`, or `not run`. |
| Applied | `Yes` or `No`. |
| Validation | `PASS`, `FAIL`, or `not run`. `PASS` means the printed line `===== OVERALL: PASS =====`. |
| Backup | The printed name `dbo.<ReleaseToken>_Backup_<ObjectToken>_<RunTag>`, or `not created (<reason>)`. |
| Rollback | `not required`, `referenced`, or `executed`. |
| Result | `PASS`, `FAIL`, or `STOP`. |
| Evidence File | Path of the copied evidence file for this RunTag. |

`DryRun = PASS` means the preview completed and matched the execution gate. It does not mean the change was written. **Applied** carries that fact.

**Rollback** `referenced` means the `c` script is named on the evidence file and was not run. `executed` means that script committed. `not required` is allowed only when the package made no change and the evidence file still names the `c` script.

**Result** follows the evidence file. A dry-run `STOP` stays `STOP` on its own row after a later RunTag passes. A validation `FAIL` is **Result** `FAIL`. A row is `PASS` only when **Validation** is `PASS`.

CAB holds recorded on the evidence file stay in force when **Result** is `PASS`. They are not a column on this matrix.

---

## Row rules

- Append a row when the dry-run batch returns, then fill **Applied**, **Validation**, **Backup**, and **Result** when those later batches return. When the apply uses a new RunTag, append a new row instead of reusing the dry-run row.
- **Backup** `not created` needs a reason: `dry run`, `already applied`, `blocked`, `verify found nothing to change`, or `template latch`.
- Leave no cell blank on a finished row.
- Stop the window when **Validation** is `FAIL`, when **Result** is `STOP`, when the session catalog differs from the expected catalog, or when the evidence file records a SHA-256 mismatch.
- Point **Evidence File** at the copy under `docs/reviews/RC019/executions/`. Raw prints live under `docs/reviews/RC019/evidence/` and are named from that evidence file.
