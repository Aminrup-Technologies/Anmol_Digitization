# Exact execution order

**Additive notice, 2026-11-18.** Do not start steps 2 or 4. `AnmolDristi` already has the three history columns and KM module `F6B07BE8…`, matching `AnmolDristiDev`. The only read to repeat is `11_Current_State_ReadOnly.sql`. `10_Preflight_ReadOnly.sql` and `50_Postflight_ReadOnly.sql` still expect the 2026-11-17 discovery snapshot and the package evidence row. On the current catalog they fail or throw. That result is not a reason to apply or roll back. File `40` stays excluded. The historical order below is retained.

**Closure status: DOCUMENTATION UPDATED — NO-CHANGE DEPLOYMENT DECISION PENDING FORMAL ACCEPTANCE.** For the current no-change outcome, scripts `20`, `30`, `40`, `60`, and `61` are NOT TO BE EXECUTED. Formal acceptance of `DR-NOCHANGE` is unsigned.

**Status: BLOCKED.** Do not start this order against `AnmolDristi` on this revision. Scripts `20`, `30`, `60`, and `61` abort on unsigned gates. `40_WP03_ActivityCounters.sql` is `EXCLUDED — DO NOT EXECUTE` and is not a step.

Run from `Database/Deployments/TA_DA_LIVE_GoLive_20261117/`. One file per `sqlcmd` process. Flag set: `-S MUMANMOAPP01\SQLEXPRESS -d AnmolDristi -f 65001 -b -I`. The connection is the approved login, after the applicable rows in `03_Decision_Gates.md` are signed and the gate literals have been revised. No login is approved in this revision.

| Step | File | Writes | Stop the window if |
| --- | --- | --- | --- |
| 0 | None. Complete backup, `RESTORE VERIFYONLY WITH CHECKSUM`, and the isolated restore. The evidence table is a separate DBA prerequisite and is not this step | No | Any verify or restore failure, a restore onto `AnmolDristi`, or creation of the evidence table by the deployment login |
| 1 | `10_Preflight_ReadOnly.sql` | No | Any failure row. `PARKED_WP03` rows do not fail this step. Save the result grids, including resolver hashes and the claim module text |
| 2 | `20_WP01_Schema.sql` | One evidence row and three nullable columns | Any throw. Do not continue to step 3 |
| 3 | `50_Postflight_ReadOnly.sql` | No | A `FAIL` row. `NOT_APPLIED` for WP-02 is expected here. `EXCLUDED_UNCHANGED` for `usp_RecalculateApprovalRequest` is expected |
| 4 | `30_WP02_KM_Procedure.sql` | Replaces the KM module. Requires the three columns | Any throw, including `NOT-FILED` for the claim module or blank resolver hashes |
| 5 | `50_Postflight_ReadOnly.sql` | No | WP-02 is not `APPLIED`, WP-01 is not `APPLIED`, the recalc fingerprint moved, or an excluded module hash moved |

WP-02 requires WP-01 because the `F6B07BE8` insert names the three columns. File `40` is not between these steps and is not after them.

## Rollback order

Use this only after a committed apply, and only with `60` and `61` from the same reviewed revision. `60` does not restore `usp_RecalculateApprovalRequest`.

| Step | File | Effect |
| --- | --- | --- |
| R1 | `60_Rollback_Procedures.sql` | Restores only the WP-02 KM preimage when the current module is exactly `F6B07BE8` and the saved text is exactly `EC6A97B9`, length 15455. Aborts when the saved text contains `CategoryId` unless that exception literal is the signed value. Aborts when the saved header is not one `CREATE   PROCEDURE` token. Does not `DROP`, `GRANT`, delete claim rows, or recompute amounts |
| R2 | `61_Rollback_WP01_Columns.sql` | Drops the three columns only when every value is NULL, the definitions match, the preimage row says the columns were absent, and no module or dependency still references them. Otherwise it leaves the columns in place |

If R1 refuses the KM preimage, do not force R2. The candidate procedure still references the columns, so R2 aborts and the history rows stay.

A module rollback does not reverse a kilometre save that already committed. That repair is outside this package.

Rollback of the LIVE header is not proven by this revision. The preimage bytes are not in the repository. `60` stays blocked on the unsigned CategoryId literal and on the unsigned identity literal.
