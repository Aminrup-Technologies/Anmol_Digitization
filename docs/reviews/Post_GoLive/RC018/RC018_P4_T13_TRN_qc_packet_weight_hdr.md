# RC-018 Phase 4 / Table 13 — TRN_qc_packet_weight_hdr

**Status:** Scripts generated — **not executed**  
**Risk:** Medium  
**Date:** 2026-09-25  
**Source:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`  
**Target:** LIVE `AnmolDristi.dbo.TRN_qc_packet_weight_hdr`  
**Mode:** Verification only. No TA/DA workflow reads this table.

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Row count | 3,797 | 37,952 | Preserve LIVE. Do not equalize. |
| Shared columns (26) | Match | Match | None |
| `FormID` int NULL, default `((21))` | Absent | Present | **Retain.** Do not alter. |
| PK | `PacketHdrID` | `PacketHdrID` | None |
| Identity | `PacketHdrID (1,1)` curr=3,797 | curr=37,952 | Do not reseed |
| PK index `PK__TRN_qc_p__12D873D48888808D` | Yes | Yes | None |
| `FK_QCInspector_PacketHdr` | Yes | Yes | None |
| Non-PK index differences | None in Phase 0 | None in Phase 0 | None |
| Triggers | None | None | None |

---

## 2. Verification / reconciliation patch

`Database/Scripts/028_RC018_P4_T13_TRN_qc_packet_weight_hdr_Verify.sql`

- Default `@DryRun = 1`.
- When shared columns, `FormID` and its default, the primary key, identity, and the inspector foreign key all match, it returns with no backup and no DDL.
- It never issues `ALTER` on `FormID`.
- `RC018_Backup_TRN_qc_packet_weight_hdr_<RunTag>` is created only when `@DryRun = 0` and a genuine gap was printed. The script still does not change the table.

---

## 3. Validation SQL

`Database/Scripts/028b_RC018_P4_T13_TRN_qc_packet_weight_hdr_Validate.sql`

Proves:

- All 26 shared columns exist  
- `FormID` exists and its default still contains 21  
- PK is `PacketHdrID` and the Phase 0 PK index remains  
- `PacketHdrID` is still an identity  
- `FK_QCInspector_PacketHdr` is present and trusted  
- Row count is at least 37,952  
- `DBCC CHECKCONSTRAINTS`

---

## 4. Rollback SQL

`Database/Scripts/028c_RC018_P4_T13_TRN_qc_packet_weight_hdr_Rollback.sql`

No data or schema is undone. The aligned path changes nothing. A review backup is not loaded back over production packet-weight headers, and `FormID` stays as it is.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Medium** |
| Why | Production QC headers. `FormID` is LIVE-only and must keep default 21. |
| This package | Read-only when the shared schema matches |
| Data change | None |
| Downtime | None on the aligned path. A gap backup copies about 37,952 rows. |
| Rollback | Not applicable unless a later, separate change is approved |

---

## 6. PASS checklist

| Check | Result (fill at exec) |
| --- | --- |
| Shared schema unchanged (26) | ☐ |
| FormID retained with default ((21)) | ☐ |
| PK unchanged | ☐ |
| PK index remains | ☐ |
| FK remains | ☐ |
| Row count floor 37,952 | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open `TRN_qcinspector` until Overall = PASS.

---

## Suggested execution order (when approved)

1. `028_*_Verify.sql` with `@DryRun = 1`. Expect `Alignment gaps=0` and no backup.  
2. `028b_*_Validate.sql`. Require Overall **PASS**.  
3. Leave `@DryRun = 0` unused unless the verify script reports a gap.
