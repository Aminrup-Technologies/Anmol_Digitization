# RC-018 P2-T06A — PolicyException Business Impact (CAB)

**Status:** Report only. No SQL was changed or executed as a patch.  
**As-of date for “today”:** 2026-09-25  
**Sources:** `Database/Scripts/021_RC018_P2_T06_PolicyException_Patch.sql`, `RC018_P2_T06_PolicyException.md`, `PolicyResolutionService`, `PolicyExceptionDataAccess.GetApplicablePolicyException`, `PolicyOverrideMergeHelper`  
**LIVE baseline:** `usp_GetResolvedPolicy` on `AnmolDristi` for AHS1562, AHS628, AHS1530. `PolicyException` count = 0.

---

## How precedence actually works

`usp_GetResolvedPolicy` reads `PolicyMatrix` only. It does not read `PolicyException`. Its result set does not change when these four rows are inserted.

Claim screens use `PolicyResolutionService.ResolvePolicy`, which calls that procedure and then overlays one exception:

- Active (`IsActive = 1`)
- `EffectiveFrom` on or before the claim date
- `EffectiveTo` null or on or after the claim date
- If several match, `ORDER BY Priority ASC` (lower number wins) and `TOP 1`

Override JSON replaces only keys that are present. Missing keys stay on the matrix values. Stored Region/Branch/Grade on the exception row are not used in this lookup.

The matrix row returned for all three employees today is **PolicyId 6**:

| Field | Matrix value |
| --- | ---: |
| MaxKM (monthly km cap) | 160 |
| RatePerKm (bike travel rate) | 3.75 |
| DA (HQ / Ex-HQ) | 225 |
| Outstation DA | 350 |
| Own arrangement DA | 750 |
| Hotel cap | 600 |

`FixedTravelAllowance` is not in the matrix JSON or any exception JSON. `MST_EmployeeTolerance` is a different table and is not written by this patch.

---

## Row verdicts

| ExceptionId | Employee | Window | Active | Applies on 2026-09-25 | Behavioral change |
| ---: | --- | --- | --- | --- | --- |
| 1 | AHS1562 | 2026-04-01 to 2027-04-30 | Active | Yes | **Yes** |
| 2 | AHS628 | 2026-04-01 to 2027-03-31 | Active | Yes | **Yes** |
| 4 | AHS1562 | 2000-01-01 to 2000-01-02 | Inactive | No | **No** |
| 90002 | AHS1530 | 2026-08-01 to 2026-12-31 | Active | Yes | **Yes** |

---

## Flagged changes only

Amounts are rupees except MaxKM, which is a monthly kilometre cap.

### ExceptionId 1 — AHS1562 Krishna Saha

Precedence: sole active in-window exception, Priority 1. Beats PolicyId 6 for every key present in the JSON.

| Component | LIVE now | After apply | Changed |
| --- | ---: | ---: | --- |
| TA rate (`RatePerKm`) | 3.75 | 5.75 | Yes |
| DA | 225 | 250 | Yes |
| Hotel cap | 600 | 1000 | Yes |
| Outstation DA | 350 | 450 | Yes |
| Own arrangement DA | 750 | 800 | Yes |
| Monthly KM cap (`MaxKM`) | 160 | 2250 | Yes |
| KM modification tolerance (`MST_EmployeeTolerance`) | unchanged | unchanged | No |
| Fixed travel allowance | not in policy JSON | not overridden | No |

`ResolvePolicy` output: `IsPolicyExceptionApplied = true`, `PolicyExceptionId = 1`, `Source = EX`.  
`usp_GetResolvedPolicy` output: unchanged.

**Business owner required:** yes. Technical author on the row is `AHO445`. No business-owner field exists on the table.

**Rollback impact:** removing the row returns claim entry for AHS1562 to PolicyId 6 for claim dates in this window. Amounts already saved on existing claims are not rewritten.

### ExceptionId 2 — AHS628 Anjan Kumar Subudhi

Precedence: sole active in-window exception, Priority 1.

| Component | LIVE now | After apply | Changed |
| --- | ---: | ---: | --- |
| TA rate (`RatePerKm`) | 3.75 | 5.00 | Yes |
| DA | 225 | 230 | Yes |
| Hotel cap | 600 | 1000 | Yes |
| Outstation DA | 350 | 500 | Yes |
| Own arrangement DA | 750 | 1000 | Yes |
| Monthly KM cap (`MaxKM`) | 160 | 2500 | Yes |
| KM modification tolerance | unchanged | unchanged | No |
| Fixed travel allowance | not overridden | not overridden | No |

`ResolvePolicy` changes the same way, `PolicyExceptionId = 2`.  
`usp_GetResolvedPolicy` does not change.

**Business owner required:** yes. Technical author is `AHO539`.

**Rollback impact:** AHS628 claim entry returns to PolicyId 6. Saved claim snapshots stay as stored.

### ExceptionId 4 — AHS1562 (inactive)

JSON would set DA to 99 and replace remarks, but the row is inactive and its window ended on 2000-01-02. The lookup never selects it.

| Component | Change on 2026-09-25 |
| --- | --- |
| TA, DA, Hotel, Outstation DA, own arrangement, MaxKM | **None** |
| `ResolvePolicy` / `usp_GetResolvedPolicy` | **None** |

**Business owner required:** no, for current behavior. If someone later sets `IsActive = 1` inside a window that overlaps ExceptionId 1, both have Priority 1 and `TOP 1` has no tie-break. That is not the case today.

**Rollback impact:** none on calculated policy. The row would simply not be present.

### ExceptionId 90002 — AHS1530 Jyotirmaya Pradhan

Precedence: sole active in-window exception, Priority 1. JSON has no Outstation DA or own-arrangement key, so those stay on the matrix.

| Component | LIVE now | After apply | Changed |
| --- | ---: | ---: | --- |
| TA rate (`RatePerKm`) | 3.75 | 6.95 | Yes |
| DA | 225 | 600 | Yes |
| Hotel cap | 600 | 2000 | Yes |
| Outstation DA | 350 | 350 | No |
| Own arrangement DA | 750 | 750 | No |
| Monthly KM cap (`MaxKM`) | 160 | **3** | Yes |
| KM modification tolerance | unchanged | unchanged | No |
| Fixed travel allowance | not overridden | not overridden | No |

`ResolvePolicy` sets `PolicyExceptionId = 90002`.  
`usp_GetResolvedPolicy` does not change.

The monthly cap drops from 160 km to 3 km. That is the largest operational change in this package and should be confirmed before apply. Technical author is `AHO539` (created 2026-09-22).

**Business owner required:** yes.

**Rollback impact:** AHS1530 claim entry returns to PolicyId 6, including MaxKM 160. Saved snapshots are not rewritten.

---

## CAB decision

| Decision | Recommendation |
| --- | --- |
| Apply schema (index + two checks) | No policy-output change. Safe with the data gate. |
| Insert ExceptionId 4 | No current behavior change. |
| Insert ExceptionId 1 and 2 | Behavior change on TA rate, DA, hotel, outstation DA, own arrangement, and monthly KM cap. Needs a named policy owner. |
| Insert ExceptionId 90002 | Same class of change, plus monthly KM cap 160 → 3. Hold unless the owner confirms that cap. |
| Expect `usp_GetResolvedPolicy` to change | No. Judge the gate on `ResolvePolicy`, not the procedure result set. |
