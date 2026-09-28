# RC-019 execution evidence templates

These four files are the blank forms for RC-019 execution evidence. Script 032 and later consume them by copying. A run does not edit the files in this folder.

| File | Copied when | Copy lives in |
| --- | --- | --- |
| `RC019_Execution_Evidence_Template.md` | Once per RunTag | `docs/reviews/RC019/executions/` |
| `RC019_Execution_Matrix_Template.md` | Once per execution window | `docs/reviews/RC019/executions/` |
| `RC019_Rollback_Register_Template.md` | Once per execution window | `docs/reviews/RC019/executions/` |
| `README.md` | Never | This folder |

`docs/reviews/RC019/evidence/` receives the raw SQL prints named by the evidence copy. It starts empty.

Scripts `016` through `031c` stay as they are. `docs/reviews/Post_GoLive/RC018/` stays as it is. This folder does not restate an RC-018 hash and does not reopen that release.

---

## What a copy is

`Database/Scripts/031_RC019_Execution_Template.sql`, `031b_RC019_Validation_Template.sql`, and `031c_RC019_Rollback_Template.sql` are the SQL skeletons. A derived script is a new number, 032 or later, copied from those skeletons and filled under its own execution gate.

That derived script is what these forms record. An unfilled 031 file returns before it writes and before it can print `OVERALL: PASS`. Do not open an evidence copy for the template scripts themselves.

---

## How to open a window

1. Copy `RC019_Execution_Matrix_Template.md` to `docs/reviews/RC019/executions/RC019_Execution_Matrix_<Window>.md`.
2. Copy `RC019_Rollback_Register_Template.md` to `docs/reviews/RC019/executions/RC019_Rollback_Register_<Window>.md`.
3. Fill the window tables on those two copies. Use one window token for both names, for example the date the window opens (`20261001`).
4. Leave the two templates in this folder byte-for-byte as they were.

`<Window>` is chosen once, at the start. It is not a RunTag.

---

## How to record one script

Do this for each approved script number from 032 upward. Finish the row before the next number starts.

1. Copy `RC019_Execution_Evidence_Template.md` to:

   `docs/reviews/RC019/executions/RC019_<Script>_<RunTag>_Evidence.md`

   Use a temporary file name if the RunTag is not known yet. Rename the copy to the RunTag printed on the banner before the row is called finished. Do not put the filled file back into `templates/`.

2. Recompute SHA-256 for the apply, validation, and rollback files on the deployment workstation. Write both hashes on the evidence copy. Continue only when they match.

3. Run the apply script with `@DryRun` left at `1`. Paste the banner, the `COMPLETE` line, and the end time into the dry-run section.

4. Set `@DryRun = 0` in that session only when the dry run passed and the execution gate allows the write. Paste the apply banner, the `COMPLETE` line, the backup name, and the end time.

5. Run the `b` validation script. When a backup exists, point `@BackupSchema` and `@BackupName` at it. Paste the checklist and the `OVERALL` line.

6. Name the `c` rollback script on the evidence copy. Leave `@Confirm = 0` unless that gate is actually rolling this script back.

7. Save the message output under `docs/reviews/RC019/evidence/` as:

   `RC019_<Script>_<RunTag>_dryrun.txt`  
   `RC019_<Script>_<RunTag>_apply.txt`  
   `RC019_<Script>_<RunTag>_validate.txt`

   Skip the apply file when apply did not run, and say so on the evidence copy.

8. Append or complete the matrix row on the window's matrix copy. Append the rollback-register row on the window's register copy.

9. Start the next script number only when this evidence copy shows `===== OVERALL: PASS =====`.

A second batch of the same script, including a dry run that stopped and a later apply, prints a new RunTag. Repeat from step 1. Keep the earlier evidence file.

---

## What stays on the template

- Placeholders stay empty.
- Allowed values stay the lists in each template.
- Folder roles stay as this file describes them.
- A correction discovered during a run is written on the copy in `executions/`, or queued as a later change to these templates under its own review. It is not typed into the template in the middle of a window.

The SQL skeletons follow the same rule. `@DryRun = 1` and `@Confirm = 0` remain the file defaults. A session that sets either switch does not save that edit into `Database/Scripts/`.

---

## Fields the forms require

These are the RC-018 execution facts, required on every RC-019 run so a later closeout does not have to reconstruct them.

| Fact | Where it is recorded |
| --- | --- |
| Release, script number, server, database, expected catalog | Evidence identity, and the matrix window |
| Operator | Evidence identity, and the register |
| RunTag from the banner, never invented | Evidence, matrix, register |
| Start and end, both clocks | Evidence dry-run, apply, and validation sections |
| UTC stamp from `SYSUTCDATETIME()` on the banner | Evidence, separate from the local RunTag |
| SHA-256 of apply, validation, and rollback before execution | Evidence section 2 |
| Dry-run output before any `@DryRun = 0` | Evidence section 4, matrix **DryRun** |
| Apply output, or the reason apply did not run | Evidence section 5, matrix **Applied** |
| Backup table name, or an explicit reason none exists | Evidence section 6, matrix **Backup**, register |
| Validation checklist and `OVERALL` line | Evidence sections 7 and 8, matrix **Validation** |
| Next script waits for `OVERALL: PASS` | Evidence section 8, matrix row rules |
| Rollback script named even when it was not run | Evidence section 9, matrix **Rollback** |
| Backup left intact; safety copy recorded if taken | Evidence section 9, register |
| Validation after a committed rollback | Evidence section 9, register |
| CAB notes kept apart from PASS | Evidence section 10 |
| One evidence file per RunTag, including a superseded dry run | `executions/` naming |
| Full database backup recorded separately from the script backup table | Evidence pre-flight, matrix window |
