# R3 H Conserved-Inventory Feasibility 1

This test-only harness implements the bounded H investigation in
[Equilibrium Reconstruction Planning 1](M10_FINAL_VR2_R3_EQUILIBRIUM_RECONSTRUCTION_PLANNING1.md).
Current execution status and next work are recorded in [PROJECT.md](PROJECT.md).
Production and all preceding tests remain byte-identical. This harness cannot
install a production seed or authorize R3, C/J, retuning or threshold changes.

## Implementation and preexecution review

Three new test files implement the adapter, the generic bounded numerical search
and its focused/explicit tests. No production seam, registered scenario, controller
initial state, transport model, C4 data or default closure changes.

The adapter uses the existing operational-seed factory for seed-step1. It constructs
the same public integrated solver from canonical definitions, mode-2 thermodynamics
and the existing full-plant instrumentation catalog, advances the unmodified state
through seed-step2, and wraps that committed result in the normal runtime at logical
step 0. One Running step supplies the last objective checkpoint. No reflection,
state reset, extra settling, controller takeover or physical re-integration is used.
The preexecution test compares node states against frozen Diagnostic 1 and compares
the reconstructed post-seed/Running canonical state with the unmodified factory.
It also repeats the fresh recipe to check deterministic observation.

Only the 24 raw mass/energy scalars vary, in fixed node/mass/energy order. The unchanged
C4 resolver derives phase/pressure/temperature/quality for each trial. Search scales,
bounds, 75 residuals, numerical method, damping, pivot guard, maximum iterations and
evaluation budget are copied verbatim from the SHA-authenticated planning contract.
The first feasible scaled root ends the search. An invalid finite-difference probe,
exhausted damping/line search, iteration limit or recipe budget returns the planned
inconclusive outcome; it cannot initiate another trim family or enlarge a bound.

Every attempted evaluation reserves a budget slot before its bounds/domain checks,
so rejected out-of-bounds attempts conservatively consume the 4096 limit even though
they do not call physics. Fresh constructor/baseline checks and fixed confirmation
runs are separately identified. Each trace entry records all coordinates, all 75
scaled residuals when valid, maximum objective, acceptance and rejection reason.
Valid entries also record the 48 phases: raw, seed-step1, post-seed and Running step 1,
with the 12 nodes in the frozen order. Pivot ties retain the original ordinal row
identity after swaps; no fallback conditioning algorithm is introduced.

Six preexecution tests cover baseline equality, a coupled linear residual with a
known bounded root, trace reproducibility/strict descent, failed-probe accounting,
budget exhaustion, rejection without coordinate clipping, and the frozen pivot guard.
The explicit physical search is excluded from the ordinary suite. Its entry point
requires an opt-in variable plus a successful review bound to both the current
contract SHA and the actual test assembly SHA. Source hashes are rechecked in-process.

## Execution and evidence

```powershell
.\scripts\run-r3-h-feasibility1.cmd
```

The runner performs static source/prerequisite checks, a non-incremental Release
build with warnings as errors, the six tests and a structured xUnit report review.
Only after those pass does it issue the preexecution token and invoke the explicit
search. A final validator independently checks the complete trace and classification.
Technical failure produces no successful adjudication manifest. Old named outputs
are invalidated before each execution; unrelated evidence is retained.

Artifacts under `artifacts/r3-h-feasibility1` include build/test logs, baseline
observations, the preexecution review, JSONL search trace, best observed inventory,
result and stop reason, confirmations, execution summary and SHA-256 manifest.
The best observed vector is explicitly not a qualified seed when no root is found.

If a scaled root is found, three fresh recipes perform the planned 100 Running steps.
They record all-step health/state, sampled fluid residuals, rotor torque, thermal
inventory, neutron/precursor/poison state, controller memory/output and mechanical/
electrical state. Their serialized trajectories must reproduce bit-for-bit on the
same runtime. Every sampled storage/torque residual must stay within its frozen
scale and every Running step must pass the unchanged envelope. Existing whole-plant
mass/energy conservation guards remain `1e-6 kg` and `1e-2 J`, from the authoritative
Exact-V9 audit. Passing those observations does not establish long-term stationarity.

No root yields `H-INCONCLUSIVE-NO-CANDIDATE-IN-BOUNDS`, with zero confirmation runs;
it is neither proof that no root exists nor authority to retune the numerical plan.
A root that fails examination yields `H-CANDIDATE-NOT-QUALIFIED`. A fully passing
examination yields `H-FEASIBLE-FLUID-SEED`, permitting returned-evidence review and
separate integration planning only. Unexpected harness exceptions fail technically.
R3 remains RED for every result of this gate.

Complete ordinary regression and current-evidence audits remain required for this
test-only implementation and are reported separately from the physical-search result.
