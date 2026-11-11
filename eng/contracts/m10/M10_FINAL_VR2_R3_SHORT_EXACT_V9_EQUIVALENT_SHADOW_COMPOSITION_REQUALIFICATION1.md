# M10 Final — VR2 — R3 Short Exact-v9-Equivalent Shadow / Composition Requalification 1

## Status
**EXECUTION CANDIDATE — TEST ONLY**

Prerequisites: R2 returned evidence PASS and R3 Planning 1 returned `PASS-AS-AUTHORED`, both frozen in the candidate.

## Purpose
This gate tests whether the already qualified production closure mode `ReferenceConsistentTabulatedInverseDomain = 2` can be composed into an otherwise exact-v9-equivalent runtime without breaking the established short deterministic operating contract.

It does not mutate canonical exact-v9 and does not activate mode 2.

## Single-factor proof
The focused Application test creates:
1. canonical exact-v9 through `DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory`;
2. a test-local exact-v9 shadow reconstructed through `ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed` with closure mode 1;
3. the same shadow with only `thermodynamicClosureMode` changed to mode 2.

Canonical exact-v9 and the mode-1 shadow must match for all 128 running-step snapshot fingerprints before any mode-2 result can be accepted.

## Mode-2 short requalification
The mode-2 shadow runs exactly 12,000 fixed 10 ms steps (120 simulated seconds) with no operator or load-demand change.

The inherited acceptance envelope is unchanged:
- electrical export 4.99–5.01 MWe;
- primary pump mass flow 99.9–100.1 kg/s;
- drum level 0.49–0.51;
- governor output 29.27–29.30%;
- zero trip, breaker-open, rollback, fallback-commit, unsafe-commit and untargeted-disagreement steps;
- moisture drain > 0 kg/s;
- commanded-transfer mismatch <= 1E-08 kg/s;
- turbine-stage energy-ownership residual <= 1E-03 W;
- mass closure <= 1E-06 kg;
- full-energy closure <= 1E-02 J;
- balance mass-rate residual <= 1E-08 kg/s;
- balance power residual <= 1E-03 W.

Two fresh mode-2 shadows must also produce the same 128-step deterministic fingerprint.

## Scope
Exactly one new test source is authorized. No `src/` file and no historical test may change.

The gate does not switch the default closure mode, modify canonical exact-v9, create a new exact version, execute R4 long materiality, change thresholds, or authorize VR3/P3-R1/second replacement-long.

## Evidence
Exactly seven artifacts are required:
`01-contract-and-provenance.txt`, `02-shadow-baseline-equivalence.csv`, `03-mode2-shadow-health-trajectory.csv`, `04-ownership-conservation-summary.txt`, `05-deterministic-repeat.txt`, `06-r3-requalification-summary.txt`, `07-prequalification-review.txt`.

Success classification: `PASS-R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFIED`.

Returned evidence adjudication is mandatory before R4 planning.
