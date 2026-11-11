# M10 Final VR2 — R3 Seed Integration Two-Seed-Step Preconditioning Divergence Diagnostic 2

## Status

**EXECUTION CANDIDATE — TEST-ONLY**

This gate is the only engineering activity authorized by the returned/adjudicated Fast-Gate Dynamic Equilibrium Diagnostic 1. It does not authorize a production repair, seed retuning, threshold change, R3 Short Requalification 3 or R4 Planning 1.

## Purpose

Localize the first mode-1 versus reference-consistent mode-2 displacement inside the canonical 20 ms seed-preconditioning window.

Diagnostic 1 established that the raw reference-consistent conserved-inventory vector is healthy, while the post-preconditioning state is already displaced before the first normal `Running` step. Diagnostic 2 therefore separates the versioned initialization path into three exact checkpoints:

1. raw authored/reference-consistent state before runtime preconditioning;
2. state committed after deterministic seed step 1 at 10 ms;
3. state committed after deterministic seed step 2 at 20 ms / logical STEP 0.

The gate is localization evidence only. It must not choose or implement a repair.

## Frozen prerequisites

The diagnostic consumes already-returned immutable evidence:

- Fast-Gate Dynamic Equilibrium Diagnostic 1 returned `PASS-DIAGNOSTIC-EVIDENCE-COMPLETE` with engineering classification `PRECONDITIONING-INDUCED-PRIMARY-HYDRAULIC-OPERATING-POINT-DIVERGENCE`;
- Candidate Construction 1 remains the raw mode-2 source: 12/12 resolved nodes, 12/12 phase matches, no runtime preconditioning and no dynamic simulation;
- canonical exact-v9 mode 1 remains unchanged;
- the reference-consistent mode-2 factory remains unchanged;
- `src/`, historical tests, C4 resolver/payload, acceptance envelopes and downstream authority remain frozen.

## Diagnostic construction

The focused test performs no production-source mutation.

### Raw checkpoint

The raw comparison is reconstructed only from frozen returned evidence:

- canonical mode-1 raw conserved inventory and resolved state come from the returned authored-seed forward/inverse diagnostic;
- candidate raw conserved inventory comes from Candidate Construction 1;
- candidate resolved pressure/temperature/phase/quality comes from Candidate Construction 1 round-trip evidence.

No new raw state is solved or retuned by Diagnostic 2.

### Seed step 1 and seed step 2

The test-local reconstruction uses the exact canonical exact-v9 configuration and the exact reference-consistent candidate configuration with `deterministicSeedStepCount = 1` and `2` respectively.

As a guard against copied-configuration drift, each reconstructed step-2 state must match the corresponding existing factory state exactly for all 12 fluid-node mass, internal-energy, pressure, temperature, phase and quality coordinates, primary-circulation flows and speed-controller diagnostic state.

## Required evidence

The focused run must emit exactly:

1. `01-raw-checkpoint-node-comparison.csv` — 12 raw node rows;
2. `02-seed-step1-node-comparison.csv` — 12 node rows after preconditioning step 1;
3. `03-seed-step2-node-comparison.csv` — 12 node rows after preconditioning step 2;
4. `04-seed-step-hydraulic-heads.csv` — 8 frozen hydraulic heads at each seed step;
5. `05-seed-step-flow-comparison.csv` — 12 primary/secondary flow signals at each seed step;
6. `06-seed-step-controller-turbine.csv` — governor/controller, turbine and electrical response at each seed step;
7. `07-diagnostic-summary.txt` — evidence-only localization summary;
8. `08-pre-repair-review.txt` — explicit authority hold and return-for-adjudication marker.

The adjudicator reports, without introducing a new acceptance tolerance:

- phase-mismatch count and node identities at raw, step 1 and step 2;
- maximum absolute pressure delta at each checkpoint;
- maximum absolute hydraulic-head and flow delta at step 1 and step 2;
- `suction` phase at all three checkpoints;
- governor, speed-error and speed-integral deltas at step 1 and step 2;
- the first checkpoint at which a phase mismatch is observed.

These are diagnostic measurements, not pass/fail thresholds.

## Gate semantics

`PASS-DIAGNOSTIC-EVIDENCE-COMPLETE` means only that the three-checkpoint evidence set is complete, finite and provenance-locked. It does **not** mean R3 PASS and does not authorize a repair.

Returned evidence must be reviewed before selecting any causal seam. The review must distinguish, using the actual checkpoint data, among the still-open mechanisms from Diagnostic 1:

- phase-boundary / branch sensitivity during preconditioning;
- closure-tangent divergence under the first common perturbation;
- an exact-v9/mode-1-specific preconditioning-path assumption that is not equilibrium-preserving for mode 2.

## Authority

Still forbidden after a successful Diagnostic 2 execution until returned evidence is explicitly adjudicated:

- raw seed retuning;
- threshold or envelope change;
- C4 resolver/payload change;
- canonical exact-v9 change;
- production repair;
- new exact-version identity;
- R3 Short Requalification 3;
- R4 Planning 1;
- VR3, P3-R1 or a second replacement-long baseline.

R3 remains **RED** and R4 remains **BLOCKED**.

## Execution

From the repository root in PowerShell:

```powershell
.\scripts\run-m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2.cmd
```

Return the complete artifact directory:

`artifacts/m10-final-physical-reference-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2`

before any repair planning or implementation.
