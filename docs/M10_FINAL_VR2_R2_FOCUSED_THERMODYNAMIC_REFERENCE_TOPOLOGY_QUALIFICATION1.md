# M10 Final — VR2 — R2 Focused Thermodynamic / Reference / Topology Qualification 1

## Status

**EXECUTION CANDIDATE — TEST/REFERENCE ONLY**

Planning 1 returned evidence is adjudicated PASS. This gate may execute exactly one new focused Simulation test against explicit production mode `ReferenceConsistentTabulatedInverseDomain = 2`. It does not change production source or activate mode 2 anywhere else.

## Qualification matrix

R2 independently requalifies the staged production mode against IAPWS/RP1A reference evidence over 40 VR2 rows, 360 frozen exact-v9 node states and 1,280 seam probes. The 288-row hydraulic corpus remains frozen context owned by R4 and is not executed here.

The physical oracle is the independent `IapwsIf97Reference` helper plus reference values frozen by RP1A. C4/shadow output is not an acceptance oracle. The focused test reconstructs the 39 inverse-applicable VR2 inputs directly from IF97 Regions 1/2/4 and verifies the frozen corpus remains reference-consistent before scoring production mode 2.

Acceptance remains exactly the Planning 1 contract: IF97 self-check `<=1e-8`; zero unresolved/phase mismatches; M10-core pressure and absolute-Kelvin temperature relative error `<=25%`; compressed-liquid plus exact-v9 pressure target `<=10%`; exact-v9 phase agreement 100%; all 1,280 seam probes resolved with zero phase mismatch; inherited C3/C4 seam-continuity maxima not exceeded; deterministic repeat exact.

The test does not run an exact-v9 scenario, compose mode 2 into an exact-v9 factory, run hydraulic long materiality or mutate any threshold/corpus coordinate.

## Required evidence

Exactly nine files are required under `artifacts/m10-final-physical-reference-vr2-r2-focused-thermodynamic-reference-topology-qualification1`:

```text
01-contract-and-provenance.txt
02-reference-selfcheck.csv
03-vr2-reference-point-qualification.csv
04-exact-v9-topology-qualification.csv
05-seam-topology-continuity.csv
06-topology-qualification-summary.txt
07-deterministic-repeat.txt
08-r2-qualification-summary.txt
09-prequalification-review.txt
```

A local PASS is qualification evidence only. Complete returned evidence must be independently adjudicated before R3 planning. Default mode 2, exact-v9 activation, VR3, P3-R1 and a second replacement-long baseline remain unauthorized.
