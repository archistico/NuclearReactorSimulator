# M10 Final — VR2 — R2 Focused Thermodynamic / Reference / Topology Qualification — Planning 1 — Returned-Evidence Adjudication

## Status

**PASS — RETURNED PLANNING EVIDENCE ADJUDICATED**

The complete four-file Planning 1 artifact set was returned after the local `PASS-AS-AUTHORED` run and reviewed against the frozen Planning 1 contract.

The returned package contains exactly:

```text
01-contract-and-provenance.txt
02-qualification-matrix.csv
03-acceptance-and-successor-summary.txt
04-preexecution-review.txt
```

All four returned artifacts preserve the authored boundaries: R1 returned evidence is PASS, mode 2 remains explicit opt-in value 2, IAPWS R7-97(2012) Regions 1/2/4 remains the independent R2 reference, the frozen matrices remain 40 VR2 / 360 exact-v9 / 1,280 seam / 288 hydraulic-context rows, and no production/default/exact-v9 change is authorized by the planning run itself.

Returned artifact SHA-256 values:

```text
01-contract-and-provenance.txt  7D5FF2ECE758A45E78AA55AF29ECED017C62C0F74FB36653A9286D1AF630DA99
02-qualification-matrix.csv      1AFE1B9706F156C583ADD1768814DE9A902ADAA54F89C35B925F84FB58792788
03-acceptance-and-successor-summary.txt DCC36E9DA86A84CCE3CE32454518A2B104DB317175FE50F23688DFEBC0ED98EF
04-preexecution-review.txt       AE1FCBB2EA81AE213F5862104C71CB9E8745B9CC57DEA65C5FFA9412FA88242A
```

## Adjudication

Planning 1 is accepted as authored. **R2 execution is now authorized only for the bounded test/reference gate `R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFICATION1`.**

The execution may add exactly one new focused Simulation test and its gate/documentation infrastructure. It may not modify `src/`, any historical test, the mode-2 payload/resolver, default closure selection, exact-v9 composition, thresholds, seam coordinates or hydraulic long-materiality ownership.

R3 planning remains blocked until complete R2 execution evidence is returned and separately adjudicated. VR3, P3-R1 and a second replacement-long baseline remain unauthorized.
