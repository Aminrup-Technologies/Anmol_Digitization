# RC-018 Master Runbook

**Manifest:** `RC018_Execution_Manifest_v1.md`  
**Hashes:** `RC018_SHA256_Manifest.md`  
**Database:** `AnmolDristi` only  
**Status:** Not started  

This runbook is the operator sequence. It does not replace the script headers. Do not edit scripts 016–029 to make a step pass.

---

## Before the window

1. Take a full backup of `AnmolDristi` outside these scripts. Record the backup name and time in the execution matrix header notes.
2. On the deployment workstation, recompute SHA-256 for `Database/Scripts/016*` through `029*` and match `RC018_SHA256_Manifest.md`.
3. Open each apply script and confirm the first `@DryRun` assignment is `1`.
4. Open `021_RC018_P2_T06_PolicyException_Patch.sql` and confirm `@EnableFinancialOverrides = 0` unless CAB has signed the financial gate.
5. Confirm the session catalog with `SELECT DB_NAME();`. Stop if the result is not `AnmolDristi`.

## For every script number from 016 to 029

1. Run the apply or verify script with `@DryRun` left at 1.
2. Read the printed mode, gaps, skips, and row counts.
3. If the output shows a stop condition, do not set `@DryRun = 0`. Log the stop and follow section “When to stop”.
4. If the dry run matches the manifest’s expected effect, set `@DryRun = 0` for that execution only and run it again.
5. Copy the printed backup table name into the evidence column when a backup is created.
6. Run the matching `b` validation script. For 022b, 024b, 025b, and 026b, set `@BackupTable` when the apply script created one.
7. Require the line `OVERALL: PASS` before the next number.
8. Write start time, end time, result, and evidence file into the execution matrix.

Verification scripts 019, 023, 027, 028, and 029 are expected to print alignment gaps = 0 and skip both backup and DDL. Do not set `@DryRun = 0` on those unless a gap is printed and a reviewer accepts a large backup.

## Phase notes

**016–018.** Small reference data. 018 inserts three tolerance rows.

**019.** No MERGE unless shared attributes drifted after Phase 0.

**020.** Confirm the print says `RMF330` is excluded. Do not edit the predicate during the window.

**021.** Default success is schema objects plus ExceptionId 4 only. Do not expect four rows unless `@EnableFinancialOverrides` was approved and set to 1. If it is set to 1, AHS1530’s 3 km cap must already be confirmed in writing.

**022.** If orphans are printed, leave the foreign key uncreated. Do not add `WITH NOCHECK`.

**023.** Parent of the history detail foreign key. No snapshot or identity change.

**024.** Confirm illegal `ClaimStatus` count is 0 before `@DryRun = 0`. The check scan locks `SalesDailyActivity` briefly.

**025.** After apply, receipt and `ClaimStatus` drift versus the backup must be 0.

**026.** If duplicate open `DayLogID` rows print, stop. Do not create the unique index by hand.

**027–029.** No-op when gaps are 0. Do not copy UAT logs or drop `FormID` or `ShiftDate`.

## When to stop

Stop and do not continue to the next table when any of these occur:

- Validation is not PASS.
- DDL appears that the script header does not describe.
- 024 cannot drop or recreate `CK_SalesDailyActivity_ClaimStatus`.
- 022 prints orphan `ApprovalRequestDetailId` values.
- 026 prints duplicate open attendance keys.
- A row-count floor in the manifest is breached.
- The connection is not `AnmolDristi`.

## Rollback

Use only the `c` script for the table that just ran. Set that script’s `@Confirm` or `@BackupTable` exactly as its header says. Do not roll back earlier PASS tables unless the failed change depends on them.

024c will refuse if any row is already `ClaimStatus = Returned`. 022c and 025c will refuse to drop a column that is no longer entirely NULL. 023c, 027c, 028c, and 029c do not reload data.

If a script rollback cannot restore safely, restore the full `AnmolDristi` backup taken before the window.

## Close

The release is not certified until 016b through 029b have been run on `AnmolDristi` and the execution matrix is filled. PolicyException financial overrides, if still at 0, stay a follow-up CAB item and are not a failed deployment.
