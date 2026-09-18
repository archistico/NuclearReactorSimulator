# M10 Final — VR2 — R3 Short Exact-v9-Equivalent Shadow / Composition Requalification — Planning 1

## Status

**PLANNING-ONLY CANDIDATE**

Prerequisite: returned R2 focused qualification evidence is adjudicated PASS and frozen. This document authorizes no R3 execution by itself.

## 1. Purpose

R1 staged selected C4 semantics as explicit production closure mode `ReferenceConsistentTabulatedInverseDomain = 2`. R2 then independently requalified that production mode against IF97 and the frozen VR2/exact-v9/seam topology. R3 plans the first bounded **composition-level** check.

R3 answers one question:

> If the already-qualified mode 2 is substituted as the single thermodynamic-closure factor inside an otherwise exact-v9-equivalent runtime composition, does the short deterministic exact-v9 operating trajectory preserve the already-qualified health, conservation, controller and ownership contracts?

R3 is not an activation gate and does not reinterpret historical exact-v9.

## 2. Single-factor shadow-composition rule

The future R3 test belongs in `NuclearReactorSimulator.Application.Tests`, where the existing `InternalsVisibleTo` contract permits access to the operational seed factory without adding production hooks.

The test will construct three paths:

1. **canonical exact-v9** — the unchanged production exact-v9 factory (`integrated-operations-desktop-stable@9`);
2. **shadow baseline** — a test-local reconstruction of the exact-v9 composition through `ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed`, using the same authored exact-v9 parameters and `CorrelationConsistentInverseDomain`;
3. **shadow mode 2** — the same test-local composition with one and only one changed factor: `thermodynamicClosureMode = ReferenceConsistentTabulatedInverseDomain`.

Before mode 2 may be scored, canonical exact-v9 and the shadow baseline must prove deterministic equivalence for **128 running steps** with zero snapshot-fingerprint mismatch. Fixed timestep must remain exactly `10 ms`.

This equivalence proof makes the test-local composition an exact-v9-equivalent shadow. If the baseline shadow does not reproduce canonical exact-v9, R3 is invalid and blocking; no mode-2 result may be accepted.

## 3. Frozen provenance

The following production/application inputs remain immutable for R3:

- `DesktopSustainedGenerationInitialConditionFactory.cs`, including the post-moisture exact-v9 operating point;
- `DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.cs`;
- `ColdShutdownInitialConditionFactory.cs`;
- `DesktopHydraulicProductionPolicy.cs`;
- the historical exact-v9 120 s activation-candidate envelope and its focused audit source;
- production mode-2 implementation/payload qualified by R1/R2.

The future R3 gate may add exactly one focused Application test source. It may not modify any existing test or any `src/` file.

## 4. Short runtime workload

After the baseline-equivalence proof, the mode-2 shadow composition runs exactly:

```text
120 simulated seconds
12,000 deterministic steps
10 ms fixed timestep
ControlRoomRunState.Running
no operator/load-demand change
```

This deliberately reuses the previously qualified exact-v9 activation-candidate short health workload. It is not the P1B 5→6 MWe materiality trajectory; R4 owns that long workload.

## 5. Acceptance contract

R3 inherits, without widening, the established 120 s exact-v9 health envelope:

```text
electrical export             4.99 .. 5.01 MWe
primary pump mass flow       99.9  .. 100.1 kg/s
steam-drum liquid level       0.49 .. 0.51
governor output              29.27 .. 29.30 %
```

All 12,000 mode-2 shadow steps must be finite and must also satisfy:

```text
trip steps                                  = 0
breaker-open steps                          = 0
hydraulic rollbacks                         = 0
fallback-commit violations                  = 0
unsafe-commit violations                    = 0
untargeted branch disagreements             = 0
minimum turbine moisture drain              > 0 kg/s
max commanded-transfer mismatch             <= 1E-08 kg/s
max turbine-stage energy ownership residual <= 1E-03 W
max network mass-closure residual           <= 1E-06 kg
max full-energy closure residual             <= 1E-02 J
max balance mass-rate residual              <= 1E-08 kg/s
max balance power residual                  <= 1E-03 W
```

These are inherited qualification limits, not new thermodynamic tolerances.

## 6. Determinism

Two fresh mode-2 shadow compositions must produce the same 128-step deterministic fingerprint. The mode-2 fingerprint is **not required to equal historical exact-v9**, because mode 2 is the repair candidate and may legitimately change thermodynamic state bits. What must remain exact is:

- canonical exact-v9 ↔ shadow-baseline fingerprint equality;
- mode2-shadow run A ↔ mode2-shadow run B fingerprint equality.

## 7. Explicit non-authorizations

Planning 1 and the future R3 execution may not:

- change production/application source;
- switch the default water/steam closure mode;
- edit the exact-v9 factory or historical exact-v9 identity;
- claim that historical exact-v9 now used mode 2;
- add a new exact version;
- perform the R4 5→6 MWe long materiality replay;
- change any R2/VR2/reference/continuity threshold;
- authorize VR3, P3-R1 or a second replacement-long baseline.

## 8. Future executable gate

The future gate is:

```text
R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION1
```

It will add exactly one explicit focused test:

```text
tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/
M10FinalVr2R3ShortExactV9EquivalentShadowCompositionRequalificationTests.cs
```

The gate must run the ordinary Release suite, force a non-incremental rebuild of the focused test project, and then run the explicit R3 test.

Exactly seven execution artifacts are planned:

```text
01-contract-and-provenance.txt
02-shadow-baseline-equivalence.csv
03-mode2-shadow-health-trajectory.csv
04-ownership-conservation-summary.txt
05-deterministic-repeat.txt
06-r3-requalification-summary.txt
07-prequalification-review.txt
```

A successful local R3 execution still requires returned-evidence adjudication before any R4 planning.

## 9. Successor

Success classification:

```text
PASS-R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFIED
```

After complete returned R3 adjudication, and only then, the next planning-only successor may be:

```text
R4-P1B-EQUIVALENT-LONG-MATERIALITY-RECHECK-PLANNING1
```

No activation authority is implied.
