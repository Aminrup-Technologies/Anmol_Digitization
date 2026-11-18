# Rollback feasibility matrix

**Status: BLOCKED.** No rollback here is valid for `AnmolDristi`. A valid rollback restores the exact module text and schema captured in the preimage, keeps every history row, and refuses to run if the catalog changed after that capture.

## Rules for a later LIVE rollback

1. The preimage is the full `OBJECT_DEFINITION` and the full column metadata captured in the same window as the apply, not a body copied from a UAT script.
2. The rollback transaction restores that module with `ALTER PROCEDURE` and commits only when the stored text matches the capture.
3. History rows are not deleted. Columns that hold any non-NULL `ApprovalRequestId`, `ApproverRole`, or `ApproverLevel` stay.
4. If the procedure and the columns were applied in one transaction, the rollback of both belongs in one transaction, with a schema lock held across the row check.
5. A rollback stops when the current module is neither the applied candidate nor the captured preimage. That state needs a person, not a scripted overwrite.
6. `ReadyForPayment` rows and approval-history rows are not rewritten by the rollback of these two procedures.

## Artifact matrix

| Artifact | Intended restore | Valid for the current git candidate | Valid for LIVE | Reason |
| --- | --- | --- | --- | --- |
| `Database/Deployments/ApproverKmEdit/D_Rollback_ApproverKmEdit_UAT.sql` (untracked) | Restores the Auditor-only KM body. Declared lengths: previous LEN 15451, DATALENGTH 30902; applied LEN 16350, DATALENGTH 32700. Drops the three columns when every audit value is NULL. Keeps the columns when any audit value is present, while still restoring the Auditor-only procedure. Aborts on `AnmolDristi`. `@Execute` defaults to 0. | No | No | The applied body it expects is the `CategoryId` candidate from `B_Apply`, not the `Designation_ID` body at `HEAD`. The previous body is the Auditor-only module, which has not been shown to be the current LIVE module. Dropping columns, even when they are all NULL, removes schema that a concurrent approver save might be about to use; the script does hold `TABLOCKX` for its own check, and that lock is still a UAT behaviour on the wrong preimage. |
| `B_Apply_ApproverKmEdit_UAT.sql` re-run | Not a rollback. It installs the `CategoryId` candidate. | No | No | Re-running it after the `Designation_ID` correction would move the procedure backwards and would fail its own preimage check. The file comment on `C_Apply` says `B_Apply` must not be rerun. |
| `C_Apply_DesignationIdCorrection_UAT.sql` | Not a rollback. It moves `CategoryId` to `Designation_ID` on `AnmolDristiDev`. | It is the forward UAT correction, not a rollback. | No | It aborts on `AnmolDristi`. Its previous hash is a UAT candidate hash, not a LIVE capture. |
| `Database/Deployments/ApprovalRequestActivityCounters/` | Forward UAT apply only. No rollback file is present. | No rollback exists | No | The declared previous hash is the git module at `98947eb`, and the script says that hash was not read from a database. The comment distinguishes it from the 2026-10-13 hash `D14FEF7DFF5F8C4E008B2485D445B897CA4B76FBB60D6E5CEF68745894BE5A9D`. |
| `Database/Deployments/L2R19_UAT/D_Rollback_L2R19_Finalization_UAT.sql` | UAT finalization rollback. The activity-counter script says its own module is a different hash from that rollback preimage. | No | No | Different package, UAT catalog gates, and a hash the counter script itself treats as a different module. |
| `Database/Deployments/MarkReadyAuditorRow/Production_D_Rollback.sql` | Rollback for `usp_MarkRequestReadyForPayment` only. | No | No for this change | It does not restore the KM procedure, the history columns, or `usp_RecalculateApprovalRequest`. |
| Quoted 2026-10-14 Mark Ready hashes in `TA_DA_Deployment_Checklist.md` | A quoted record of one procedure on that date. | No | No | Those hashes were not re-read. They are not preimages of the KM or counter objects. |

## What a future LIVE rollback would have to contain

| Change | Rollback action | Data preserved | Not feasible until |
| --- | --- | --- | --- |
| Three nullable history columns | Leave the columns in place once any row has a non-NULL audit value. Drop them only when the captured preimage lacked them, the current values are all NULL, and the lock covers the check and the drop. | All history rows. Audit values when any exist. | P6 and P7 in document 04 exist, and the apply actually added the columns. |
| `usp_UpdateActivityKM_WithHistory` | `ALTER PROCEDURE` back to the captured LIVE preimage, not to the Auditor-only text embedded in `D_Rollback`. | Claim rows. The procedure writes claims only when executed; rollback of the module does not by itself recompute old saves. | P9 captured the full LIVE text. |
| `usp_RecalculateApprovalRequest` | `ALTER PROCEDURE` back to the captured LIVE preimage. | Header rows already written stay as they were at rollback time. Restoring the module does not recompute them. A later caller will use the restored formula. `ReadyForPayment` rows that the candidate refused to rewrite stay at their pre-call values only if no other writer touched them. | P10 captured the full LIVE text. There is no rollback script for `c9eba19`. |
| Amounts written by a KM save that already committed | A module rollback does not reverse `usp_RecalculateSalesClaim`. Reversing a committed save needs the history row's `OldKM` and a separately authorized financial recalc. | That reversal is outside this package. | Owner-authorized data repair, with its own backup. This review does not design that repair. |

## Concurrent-change stop

The rollback compares the live module with the applied candidate before it writes. If an administrator changed the procedure after the deployment, the rollback stops. It does not overwrite that newer text with the preimage. The same rule applies to the history columns: an unexpected type or a partial column set stops the rollback.

## Conclusion

`D_Rollback_ApproverKmEdit_UAT.sql` is an incompatible rollback artifact. The activity-counter folder has no rollback artifact. Neither may be labeled valid for LIVE. A LIVE rollback cannot be written until the preimage capture in document 04 exists.

**Additive scope note, 2026-11-18.** The executable package's rollback path is WP-02 then WP-01 only. It does not restore `usp_RecalculateApprovalRequest`. The historical row above that describes a recalc rollback stays as the earlier feasibility note. It is not a step in `60_Rollback_Procedures.sql`. The KM rollback uses `ALTER PROCEDURE` and aborts unless the saved header is exactly one `CREATE   PROCEDURE` token. The LIVE bytes are not in the repository, so that header is unproven and the rollback stays blocked.
