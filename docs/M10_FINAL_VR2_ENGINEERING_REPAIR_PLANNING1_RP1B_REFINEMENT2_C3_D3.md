# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 2 C3/D3

**Status:** VALIDATED RETURNED EVIDENCE — `PASS-EVIDENCE-MATRIX-COMPLETE`; engineering review authorizes only Refinement 3 immutable-C3 performance-tail attribution  
**Prerequisite:** RP1A REV1 validated; first-generation RP1B validated evidence; RP1B Refinement 1 C2/D2 returned `PASS-EVIDENCE-MATRIX-COMPLETE` and was reviewed as non-selecting.  
**Authority:** test/reference/shadow only. No production source change, thermodynamic repair, tolerance change, exact-v9 change, RP1C selection, VR3, P3-R1 or second replacement-long execution is authorized.

## 1. Why Refinement 2 exists

Refinement 1 materially improved both candidate families but did not produce a selectable candidate.

C2 resolves all 39 inverse-applicable VR2 rows and all 360 exact-v9 rows with 100% exact-v9 phase agreement, meets the 10% planning target and all frozen steady-state performance ceilings, but leaves 260/1280 seam probes unresolved and 55 resolved seam probes with wrong phase ownership. The residual is highly localized: two unresolved `R4-VAPOR-SIDE` probes plus 258 unresolved and 55 wrong-phase `R2-SIDE` probes.

D2 likewise resolves all core VR2 and exact-v9 rows, meets the planning target and the frozen Resolve ceilings, and has zero seam phase mismatches. Its remaining gap is even narrower: 310 unresolved probes, all on `R4-VAPOR-SIDE`; `R2-SIDE` is complete.

The response is therefore not another broad surrogate redesign. C2 and D2 are frozen evidence. Refinement 2 creates new identities that touch only the vapor-side seam problem:

```text
C3-VAPOR-SEAM-COMPLETE-SURROGATE
D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR
```

## 2. Frozen inputs

The complete returned Refinement 1 artifact set is frozen under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement1_Artifacts/
```

The engineering review summary is frozen at:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement1_UserReturnedSummary.txt
```

No RP1A coordinate is moved or regenerated. Refinement 2 reuses exactly:

```text
40 VR2 rows
  39 inverse-applicable
   1 boundary-only
360 exact-v9 node rows
288 hydraulic rows
1,280 seam probes
320 seam boundaries
4 seam sides × 320 probes
```

The seam offsets remain exactly `1e-5` relative pressure and `1e-6` quality.

## 3. C3 — vapor-seam-complete surrogate

C3 preserves C2 as an immutable primary resolver. It adds only a dense saturation-boundary discriminator for states immediately around the saturated-vapor curve.

The new discriminator is generated entirely at initialization from IF97 reference states. `TryResolve` itself does not call IF97. The dense table uses 0.02 K saturation spacing and compares the target energy with the saturated-vapor energy interpolated at the same specific volume.

This geometry is deliberately narrow:

- a small positive energy margin identifies a state immediately outside the vapor dome and returns Region 2 / `SuperheatedVapor` before C2 mixture search can steal the state;
- C2 then remains the unchanged primary resolver for all other states;
- only if C2 fails, a small negative energy margin immediately inside the saturated-vapor boundary may return a Region-4 mixture fallback.

The discriminator is bounded to ±100 J/kg around the vapor boundary, so ordinary core vapor states are not redirected through this seam-specific path.

C3 remains a table/surrogate candidate and must keep `UsesDirectIf97AtResolveTime=false`.

## 4. D3 — vapor-seam-complete IF97 comparator

D3 preserves D2 as the complete primary path. Therefore every state already resolved by D2 is returned unchanged.

Only D2-unresolved states may enter the new fallback:

1. C3 supplies a phase-aware near-vapor seed.
2. For a mixture seed, D3 performs a bounded local direct-IF97 Region-4 scan around the seed temperature.
3. A sign-changing energy residual is refined by bisection.
4. If the near-boundary direct refinement cannot improve the seed, the already phase-consistent C3 seed is retained rather than reopening broad D1-style scans.

D3 is still a fidelity/cost comparator, not a production recommendation.

## 5. Selection criterion is stricter, not relaxed

Refinement 2 preserves every existing RP1A/RP1B criterion:

```text
VR2 blocking ceiling            25%
Planning target                 10%
Exact-v9 phase agreement        100%
Resolve median ceiling          94.8 us
Resolve p95 ceiling             158.80666666666667 us
Resolve max ceiling             409.30666666666673 us
Median allocation ceiling       2816 B
```

In addition, a candidate cannot be marked RP1C-selection-eligible unless:

```text
all 1,280 seam probes resolve
AND
seam phase mismatch count = 0
```

No threshold is loosened and the seam probe coordinates remain immutable.

## 6. PASS semantics

The Refinement 2 execution gate remains an evidence-completion gate. It passes when the C3/D3 matrix is complete, deterministic and finite. A physically poor or too-slow candidate is recorded as such; it does not make the evidence harness itself fail.

For each candidate the gate emits the same nine artifact families used by Refinement 1, now with explicit `vapor_seam_completion_met` evidence in the candidate summary.

RP1C selection is not performed by this gate.

## 7. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement2.cmd
```

Return the complete folder:

```text
artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement2
```

before implementing RP1C or changing production thermodynamics.
