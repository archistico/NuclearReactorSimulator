# M10.9.7.4 Exact-V9 Cross-Host Transitive Determinism Diagnostic 1 - Returned-Evidence Adjudication 1

NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-TRANSITIVE-DIAGNOSTIC1-RETURNED-EVIDENCE-ADJUDICATION1

## Result

`PASS-AS-AUTHORED` for the diagnostic objective.

The returned local and GitHub-hosted traces contain 128 selector payloads and 128 direct-factory payloads. Selector and direct remain byte-identical per step on each host. The frozen Fingerprint V1 serializer is already cross-host stable; this adjudication concerns only the Exact-V9 128-step aggregate.

## Cross-host localization

- local aggregate: `7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`
- hosted aggregate: `1E8AAF3799059D8C9FB2E930981E26B0B31ECF465673AB6C722B24A95D80FDD7`
- divergent steps: `1 / 128`
- first and only divergent step: `126`
- steps `1..125`, `127`, `128`: byte-identical payloads
- local step-126 fingerprint: `990bb4a4a2bb90101bb285d36a9434b8ada89c5af1b7a82d5f68452a1c7a703a`
- hosted step-126 fingerprint: `7f925442d75dded8dfae66ee5f838c2ad4d94c010f51cd8fa3aea47d31c691d0`

Only top-level `turbineSecondary` differs at step 126. JSON structure, paths and kinds remain identical. Exactly five scalar leaves differ, all numeric:

1. `/turbineSecondary/rotors/0/shaftPower/numericValue`: local `5.602040801785741`, hosted `5.602040801785755`, delta `+1.4210854715202004E-14 MW`.
2. `/turbineSecondary/rotors/0/netTorque/numericValue`: local `1.9895067225661478E-05`, hosted `1.9895110881407163E-05`, delta `+4.3655745685100555E-11 N*m`.
3. `/turbineSecondary/stageGroups/0/steamFlow/numericValue`: local `13.028001865568523`, hosted `13.028001865568555`, delta `+3.197442310920451E-14 kg/s` (18 ULP at the local value).
4. `/turbineSecondary/stageGroups/0/shaftPower/numericValue`: same shaft-power delta as item 1.
5. `/turbineSecondary/totalTurbineShaftPower/numericValue`: same shaft-power delta as item 1.

The serialized first divergent leaf is rotor shaft power because rotors precede stage groups in the ControlRoom payload. The upstream visible candidate is the stage effective steam flow: stage power is derived from stage torque/effective flow and rotor power propagates from the same turbine working set.

## Classification

`SINGLE-STEP-TURBINE-SECONDARY-NUMERIC-CROSS-HOST-DRIFT`.

This evidence rules out:

- schema/shape drift;
- ordering drift;
- presentation/culture drift;
- persistent trajectory divergence;
- selector-versus-direct policy divergence;
- Fingerprint V1 serialization drift.

It does **not** yet prove whether the first internal bit drift is in commanded stage flow, inlet vapor quality, effective-flow multiplication, thermodynamic inlet state, or rotor/load arithmetic. Diagnostic 2 is required before selecting a repair owner.

## Frozen decisions

The Fingerprint V1 golden, Exact-V9 aggregate golden, production physics, tolerances, workflow and VR2/R3 branch remain unchanged. R3 remains RED and frozen while hosted ordinary CI is RED.
