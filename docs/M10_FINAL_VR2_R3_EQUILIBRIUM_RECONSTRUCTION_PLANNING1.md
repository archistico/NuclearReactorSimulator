# R3 Equilibrium Reconstruction Planning 1

NRS-MARKER:R3-EQUILIBRIUM-RECONSTRUCTION-PLANNING1

Execution status and the current continuation rule live in [PROJECT.md](PROJECT.md).
This is a planning and returned-evidence review gate. It changes neither production
nor tests and does not construct or install a new seed.

## Returned Diagnostic 1 review

NRS-MARKER:R3-EQUILIBRIUM-PLANNING1-RETURNED-REVIEW

The frozen [Residual Diagnostic 1](M10_FINAL_VR2_R3_POST_REPAIR_DYNAMIC_EQUILIBRIUM_RESIDUAL_DIAGNOSTIC1.md)
payloads are authenticated through their complete manifest and the new planning
contract. The review recomputes the observations from CSV, checks node/checkpoint
identity and verifies the recorded summary rather than accepting its labels alone.

| Observation | Supported interpretation |
| --- | --- |
| At seed-step1, candidate outlet storage is about -1.156615 MW and turbine-inlet storage about +1.006737 MW | The target-state roundtrip did not construct a whole-cycle energy equilibrium |
| Initial controller integral/output, control-valve position, rotor speed and load torque match canonical mode 1 | No independent controller-initialization defect has been demonstrated |
| Candidate rotor torque is nearly balanced at seed-step1, then becomes negative at post-seed | The secondary power deficit develops during preconditioning |
| Canonical rotor remains nearly synchronous; candidate falls to 2999.869845 rpm | Common-mode governor drift before/after Family B is not a canonical-mode invariant |
| Mass/energy conservation closes while storage residuals are large | An equilibrium defect must not be confused with lost inventory |
| All health sentinels pass but the candidate has 84 envelope violations | Evidence completeness passes; R3 does not |

The primary source terms read committed state and compose into one network step
(`IntegratedPrimaryCircuitSolver.Step`). The two 10 ms preconditioning steps are
part of initial-condition identity (`ColdShutdownInitialConditionFactory`). They
cannot be removed, extended, or used to reset drifting state. The paralleled governor
uses synchronous speed for its integral reference, distinct from its 3000.75 rpm
effective proportional/derivative setpoint (`TurbineSecondaryControlSolver`). An
integral initialized to cancel the full droop error would change that contract.

This review selects **H as the first bounded, test-only feasibility investigation**.
It does not claim H is sufficient, that a root exists, or that a unique physical
owner has been proved for every secondary energy residual. C is not selected because
fluid storage is already displaced with matching initial controller/actuator state.
J requires evidence of an independently material controller initial-state defect;
Diagnostic 1 supplies no such evidence. Neither C nor J is an automatic fallback.

## Allowed candidate surface

NRS-MARKER:R3-EQUILIBRIUM-PLANNING1-H-SURFACE

The future gate is `R3-H-CONSERVED-INVENTORY-FEASIBILITY1`, implemented only in an
explicit opt-in test/harness. It may vary raw mass and internal energy for the same
12 named fluid nodes: 24 scalar unknowns. H includes secondary fluid inventories;
restricting it to primary pressures would ignore the observed turbine-inlet residual.

Every unknown stays within +/-2% of its frozen mode-2 raw value. These bounds limit
the numerical search; they are not a physical uncertainty claim or an acceptance
tolerance. Mass must remain positive; energy/state must remain finite and resolvable
by the unchanged C4 mode-2 closure. Pressure, temperature, phase and quality are
derived from each inventory through that resolver, not separate tuning variables.
Crossing a resolvable phase boundary is recorded and is not permission to change
the phase policy, interpolation, payload or resolver fallback.

Keep all non-fluid initial conditions fixed: fuel/structure temperatures, neutron
population and precursors/poison/decay state, rods, pump and valve biases, controller
memory and modes, initial 3000 rpm rotor, breaker/load/grid, protection, geometry,
resistances, capacities, gains and every thermodynamic/transport law. In particular,
preserve Family B, vapor-limited admission with its moisture drain, two seed steps,
10 ms timestep, the requested 5 MWe load and canonical Exact-V9 identity.

The harness uses the existing operational-seed seam with an explicit test-only
inventory vector. It first reproduces the baseline factory's node, controller,
rotor/generator and actuator state exactly. It must not edit the production factory,
use reflection to mutate private runtime state, reset the candidate after stepping,
or hide a new seed behind an existing scenario/save/replay identity.

## Residual and search contract

NRS-MARKER:R3-EQUILIBRIUM-PLANNING1-SEARCH

Each evaluation starts from a fresh identical raw recipe, applies the candidate
inventories, and executes the unchanged two-step preconditioning plus one Running
step. It captures each individual 10 ms storage rate for all 12 nodes and rotor net
torque at seed-step1, post-seed and Running step 1. Average cancellation across steps
or across nodes is forbidden. The objective contains all 24 signed fluid storage
residuals and rotor torque at each of these three checkpoints: 75 residuals total.

Residual scales are frozen in the JSON before any candidate search. For each node
and inventory type the scale is the larger of the maximum absolute canonical-mode-1
rate in Diagnostic 1 and `64 * ULP(maximum canonical inventory) / 0.01 s`. Rotor
torque uses the larger of its canonical observed maximum magnitude and 64 ULP at
the maximum canonical turbine/load/loss torque. The factor 64 is a predeclared
arithmetic floor for this feasibility search, not a revised physics or R3 budget.
The resulting binary64 scales are stored as invariant roundtrip decimal strings in JSON.

Search uses dimensionless fractional changes from the frozen raw vector, central
finite differences of 1e-6, damped least squares with initial damping 1e-3, and a
deterministic ordinal variable order. Each iteration permits at most 12 successive
halvings of a full trial step; accept only a strict reduction of the maximum absolute
scaled residual. Failed/non-finite/out-of-domain/tripped evaluations are recorded
and rejected. The outer limit is 64 iterations and 4096 complete recipe evaluations,
including finite-difference probes and rejected trials. No wall-clock stopping rule,
unlogged restart, random start, gain adjustment or bound expansion is allowed.

Solve `(J-transpose J + damping I) delta = -J-transpose r` by Gaussian elimination
with largest-absolute partial pivoting and ordinal tie-breaking. A pivot divided by
the maximum absolute entry of the original damped normal matrix at or below 1e-14
rejects that linear solve. Each iteration permits six damping
attempts, multiplying damping by 10 after rejection and dividing by 10 after an
accepted step, clamped to [1e-12, 1e12]. Invalid finite-difference probes reject the
iteration; they do not become zero derivatives. Trial coordinates outside the +/-2%
bounds are rejected, not silently clipped. Exhaustion of line search, damping or
recipe budget yields an inconclusive search. Every started recipe counts against
4096, even when it fails before completing all three steps.

Stop at the first vector whose maximum absolute scaled residual is at most one;
its later 100-step examination cannot be used to restart/tune the search. Baseline
identity checks and the three fixed confirmation runs are reported separately from
the search-evaluation count. This audit authorizes implementation and preexecution
validation; execution requires the implemented harness to pass that frozen review.

## Candidate examination and outcomes

NRS-MARKER:R3-EQUILIBRIUM-PLANNING1-EXIT

A finite search result is not a qualification result. Freeze the proposed raw vector
and then run it from three independent fresh recipes. Each must reproduce the vector
and checkpoint trajectory bit-for-bit on the same runtime, preserve all non-fluid
initial conditions, and retain all health sentinels. Capture raw, seed-step1,
post-seed and Running 1, 2, 5, 10, 20, 50 and 100 plus health on every step.

`H-FEASIBLE-FLUID-SEED` requires every sampled fluid residual and rotor torque to
remain within the predeclared search scales, and every Running step to pass the
unchanged envelope: 4.99-5.01 MWe, 99.9-100.1 kg/s, drum level 0.49-0.51 and governor
29.27-29.30 percent, finite state, no trip, closed breaker and no rollback. Production
conservation errors are recorded separately and must retain their existing guards.
Observe fuel/structure energy, neutron/precursor, poison/decay state and other control
loops as well; H provides no authority to trim them or claim their long-term stationarity.

| Outcome | Required continuation |
| --- | --- |
| `H-FEASIBLE-FLUID-SEED` | Returned-evidence review, then separate seed-integration planning; no production promotion |
| `H-CANDIDATE-NOT-QUALIFIED` | Preserve vector, residuals and failed unchanged gates; return to planning |
| `H-INCONCLUSIVE-NO-CANDIDATE-IN-BOUNDS` | Preserve search trace; return to planning without claiming no root exists |
| Harness/provenance/baseline identity failure | Technical FAIL; repair the harness before interpreting physics |

No outcome authorizes C/J, a broader trim surface, physical retuning, a longer settling
window, threshold changes, production seed installation, R3 PASS, Requalification 3
or R4. Any scope expansion requires explicit evidence and a new reviewed contract.
Later seed integration must pass the frozen 100-step gate; only then can the separate
120 s / 12,000-step R3 requalification be considered.

## Artifacts and local audit

```powershell
.\scripts\run-r3-equilibrium-reconstruction-planning1.cmd
```

The audit proves frozen production and all 403 current test files, authenticates
returned evidence, recomputes review metrics/scales, and validates planning authority
and navigation markers. It emits provenance, review, residual-scale CSV, planning
summary and a SHA-256 manifest under `artifacts/r3-eq-h-plan1`.
It runs no simulation, changes no seed/controller and claims no new ordinary-suite
execution. The preceding ordinary PASS remains valid because production and tests
are unchanged. A local planning PASS permits only implementation and preexecution
validation of the bounded H test-only feasibility harness.
