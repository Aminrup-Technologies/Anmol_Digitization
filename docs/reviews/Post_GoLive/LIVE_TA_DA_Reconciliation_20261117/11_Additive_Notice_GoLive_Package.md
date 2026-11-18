# Additive notice — Go-Live package files

**Status: NOT READY.** This notice does not sign a decision and does not change documents 01–10.

On 2026-11-17 a later instruction asked for an executable package. The files are under `Database/Deployments/TA_DA_LIVE_GoLive_20261117/`.

`08_Owner_Decision_Register.md` DR-GO still says choice A would authorize a design document only, and would not authorize a script. That sentence is unchanged. The new scripts ship with unsigned gate literals and abort. They are not an execution approval, and they are not a signature on C-1, C-3, DR-COLUMNS, DR-KM, DR-COUNTER, DR-IDENTITY, DR-BACKUP, or the claim-procedure review.

`05_Rollback_Feasibility_Matrix.md` says a LIVE rollback cannot be written until the full preimage text exists. The package rollback does not invent that text. It restores only a module saved at apply time whose SHA-256 equals the fingerprint in `10_LIVE_Discovery_Capture.md`. Until that row exists, the rollback aborts.

No LIVE change was made by adding these files.

A 2026-11-18 closeout recorded that the complete LIVE `dbo.usp_RecalculateSalesClaim` text was supplied as conversation evidence. Documents 08, 09, and 10, and the package files `01`, `03`, and `05`, carry that identity. No signature field was filled. C-1 and C-3 were not selected.

**Later the same day.** Owner scope decision A excludes WP-03 from the executable path. `40_WP03_ActivityCounters.sql` is parked and throws before its historical batch. `60_Rollback_Procedures.sql` restores only the KM preimage. The claim literal in `30_WP02_KM_Procedure.sql` remains `NOT-FILED` because the claim body is not in the repository. No choice, name, or date was written into document 08. The package stays BLOCKED.

**Closure record, same day.** Document 12 and `DR-NOCHANGE` record no functional SQL difference requiring deployment. WP-01 and WP-02 are already matched. Formatting-only differences are not normalized. Scripts `20`, `30`, `40`, `60`, and `61` are NOT TO BE EXECUTED for that outcome. `DR-NOCHANGE` choice, name, and date are blank. Feature implementation for the tested scope is CLOSED. DBA/IT governance sign-off stays pending. Status: DOCUMENTATION UPDATED — NO-CHANGE DEPLOYMENT DECISION PENDING FORMAL ACCEPTANCE.
