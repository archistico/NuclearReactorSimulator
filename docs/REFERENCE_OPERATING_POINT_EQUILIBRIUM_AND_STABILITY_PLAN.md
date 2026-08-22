# Reference Operating-Point Equilibrium & Stability Plan

## Status

**PLANNING / ENGINEERING DESIGN — not yet an implementation baseline.**

This plan is the authoritative design proposal for adding explicit operating-point equilibrium diagnostics and, only after those diagnostics are trusted, a bounded reference operating-point trimmer. It is motivated by three converging observations:

1. the current simulator already distinguishes conservation closure from inventory drift;
2. the authoritative exact-v4 desktop reference passed 300 s qualification but that evidence was never claimed to prove asymptotic steady state;
3. the first M10 final long healthy leg exposed a late water/steam envelope failure on the `outlet` node, making slow operating-point drift a concrete hypothesis that must be measured rather than guessed.

The plan is deliberately split into an **immediate M10 diagnostic subset** and a **formal post-M11 M12.0 implementation**. The immediate subset may be used to diagnose the current blocker, but M10 must not acquire a general-purpose runtime trimmer merely to make the long gate pass.

## 1. Source-derived engineering rationale

Lamarsh and Baratta, *Introduction to Nuclear Engineering*, 3rd ed., Section 8.6, describe reactor thermal design as inherently iterative when neutron power, coolant density/void and heat removal are coupled. Their example repeatedly recomputes flux/power and coolant density until the density used to compute power is reproduced by the power/heat-flux solution. The engineering principle retained here is **self-consistency of coupled state**, not any PWR/BWR-specific correlation or numerical constant.

The earlier pre-M11 V&V review adds a complementary software/numerical principle: coupled owners should remain black-box canonical owners, and outer convergence/qualification should not be implemented by duplicating their constitutive equations.

### Project consequence

For Nuclear Reactor Simulator, a reference operating point should not be called physically steady merely because a 60 s or 300 s trajectory looks calm. It should be classified against explicit residuals of the actual deterministic production evolution path.

## 2. Current-project audit and terminology cleanup

The current tree already contains useful but distinct concepts that must not be conflated:

- `FullPlantReferenceOperatingPoint` is an immutable M4.7 **fixed-input reference condition**. Its source contract explicitly says it contains no hidden controller or state correction.
- `FullPlantSteadyStateCriteria` defines bounded drift/closure limits for a reference run. It is an acceptance object, not an equilibrium constructor.
- `FullPlantLongRunRunner` executes the canonical `FullPlantSolver` and measures aggregate drift without correcting state.
- authoritative desktop exact `integrated-operations-desktop-stable@4` preserves the exact-v2 physical seed and uses only a very short deterministic seed-preconditioning window before public STEP 0; that preconditioning is not a general steady-state solve.
- supervisory `HoldCurrentOperatingPoint` captures/holds operating objectives such as reactor power and turbine speed. It is an operator-control objective, **not proof that all plant inventories, controller memories and thermodynamic states are at equilibrium**.

No existing class is to be reinterpreted. Historical names and exact-version semantics remain source-compatible.

## 3. Definitions to freeze

### 3.1 Canonical discrete evolution

Let `S_n` be the complete committed state relevant to a qualification scope and let `U_n` be the authoritative inputs/commands for that step. With the production fixed step `Δt = 10 ms`:

```text
S_(n+1) = Φ_Δt(S_n, U_n)
```

The project does not need a second physical integrator. `Φ` means the already owned production path.

### 3.2 Exact equilibrium

For variables classified as stationary-required under constant target/input conditions:

```text
R(S,U) = Φ_Δt(S,U) - S
```

An exact discrete equilibrium has `R = 0` within explicitly frozen numerical representation tolerances.

### 3.3 Bounded quasi-steady state

A controlled plant may exhibit small deterministic limit cycles/deadband behavior. A state can be classified `BOUNDED_QUASI_STEADY` when:

- instantaneous residuals oscillate;
- long-window mean slopes are approximately zero within frozen budgets;
- no secular inventory/energy/domain-margin trend exists;
- all safety/conservation/coupling contracts remain green.

### 3.4 Drifting state

A state is `DRIFTING` when one or more stationary-required quantities exhibit a persistent signed slope over the qualification window, even if global mass/energy closure is excellent.

### 3.5 Settling state

`SETTLING` is a temporary classification: residual magnitude decreases toward an equilibrium/quasi-steady band and no new secular trend appears. A reference seed cannot be promoted as equilibrium merely because it is still settling within the observed window.

## 4. Equilibrium participation registry

A complete state contains values that should not all be driven to zero derivative. Every metric must be classified before acceptance criteria are written.

```text
STATIONARY_REQUIRED
BOUNDED_PERIODIC
SLOW_EVOLUTION
CYCLIC_COORDINATE
BOOKKEEPING_EXCLUDED
```

Examples:

- fluid mass/internal-energy inventories at a constant healthy operating point: `STATIONARY_REQUIRED` unless explicitly documented otherwise;
- PI controller integral memory: `STATIONARY_REQUIRED` in a true closed-loop equilibrium, bounded/periodic only if a documented deadband limit cycle exists;
- absolute rotor angle/electrical phase coordinate: `CYCLIC_COORDINATE`; phase/frequency error is evaluated instead;
- logical step, simulation time, recorder frame count: `BOOKKEEPING_EXCLUDED`;
- burnup/lifetime quantities in a future long-burn model: `SLOW_EVOLUTION`.

The registry must be machine-readable and versioned with the qualification contract.

## 5. Residual model

The inspector reports physical-unit residuals first. A dimensionless normalization may be used for ranking/optimization, but never as a substitute for the physical acceptance criterion.

### 5.1 Conserved inventories

For every fluid node:

```text
dm/dt = (m[n+1] - m[n]) / Δt
dU/dt = (U[n+1] - U[n]) / Δt
```

For thermal bodies:

```text
dU_thermal/dt
dT/dt
```

The report must separately expose:

- **physical accumulation** (`dm/dt`, `dU/dt`);
- **accounting/closure residuals** already owned by canonical balance diagnostics.

A plant can have near-zero global closure residual while individual control volumes drift. That is not a contradiction.

### 5.2 Thermodynamic/hydraulic residuals

Where supported by existing committed state/snapshot evidence:

- `dP/dt`;
- `dT/dt`;
- `dVaporQuality/dt` / `dVoidFraction/dt` where meaningful;
- path mass-flow mismatch and node net inflow/outflow;
- node energy-source/sink mismatch;
- reverse-flow state where the owner declares directionality.

### 5.3 Reactor-physics residuals

Where the state exists in the selected exact version:

- fission-power/neutron-population slope;
- total reactivity and named feedback contribution slope;
- delayed-neutron precursor slope;
- iodine/xenon inventory slope;
- decay-heat group inventory/output slope.

A slow physical state may be excluded from `STATIONARY_REQUIRED` only by an explicit registry decision, not implicitly.

### 5.4 Mechanical/electrical residuals

- rotor-speed/frequency error slope;
- net rotor torque;
- shaft-vs-electromagnetic/loss balance;
- generator output/load error;
- phase-difference behavior, not absolute angle.

### 5.5 Control residuals

For each controller:

- process error;
- integral-term slope;
- output slope;
- requested vs effective actuator position/speed;
- saturation/anti-windup state;
- automatic/manual mode consistency.

A plant that looks physically calm while a controller integral continues to ramp is not a closed-loop equilibrium.

### 5.6 Protection/fault/authority state

Healthy-equilibrium qualification requires:

- no unexpected protection actuation;
- no unexpected active fault;
- requested/effective authority consistent with the chosen qualification mode;
- no hidden true-state fallback.

These are blockers, not residuals to minimize.

## 6. Domain-headroom and trend diagnostics

The late LR-H1 failure shows that `inside envelope = true` is insufficient diagnostic information. The project should eventually measure how a supported state is approaching the boundary.

### 6.1 No duplicated thermodynamic envelope equations

Do **not** reproduce the simplified water/steam branch equations in Application or test code merely to compute a margin. The production thermodynamic model remains the authority.

### 6.2 Proposed directional support probe

For a sampled node, define the local conserved-state coordinate:

```text
x = (specific volume v, specific internal energy u)
```

and estimate the observed trend `dx/dt` from consecutive committed samples. A validation-only directional probe may use the canonical thermodynamic resolver as a support oracle along the deterministic ray:

```text
x(τ) = x + τ * dx/dt
```

with bounded deterministic bracketing/bisection to estimate the first unsupported point.

Report:

- distance in `(v,u)` coordinates;
- projected time-to-boundary **if the current local trend remained constant**;
- branch/phase near the boundary;
- explicit `DIAGNOSTIC_ONLY` quality.

This projection must never limit, clamp or alter production state.

## 7. Architecture

### 7.1 Placement

The formal inspector should live under an **Application validation/engineering boundary**, not become a new Simulation physics owner and not be exposed to the normal HMI by default.

Proposed namespace:

```text
NuclearReactorSimulator.Application.Validation.Equilibrium
```

### 7.2 Pure observation seam

Preferred core API shape:

```text
EquilibriumResidualInspector.Observe(
    previousCommittedState,
    currentCommittedState,
    currentCanonicalSnapshot,
    authoritativeInputs,
    deltaTime)
```

The inspector does not step the plant. It consumes pairs of states produced by the canonical runtime.

### 7.3 Runner owns stepping

A separate headless runner may create an exact versioned runtime, execute the canonical path and feed state/snapshot pairs to the inspector:

```text
OperatingPointEquilibriumRunner
```

This preserves:

- one physical integrator;
- exact-version factory ownership;
- deterministic step order;
- normal protection/control semantics.

### 7.4 No archive/fingerprint ownership

Residual reports are derived engineering evidence. They are not added to session archive schema v1, physical checkpoint state or fingerprint-v1 unless a separate versioned requirement is approved later.

## 8. Qualification modes

### 8.1 `CLOSED_LOOP_REFERENCE`

First and most important mode for exact-v4-style production references. Controllers, instrumentation, protection, grid coupling and the production authority seam execute normally under constant operator/external targets.

Question answered:

> Does the complete supported production composition settle to a bounded operating point, or is a controller/seed/plant inventory slowly driving it away?

### 8.2 `FIXED_INPUT_PLANT_HOLD`

A later diagnostic mode freezes an explicitly authored set of physical inputs/actuator states while observing plant inventories. It must be implemented through a validation-only factory seam that still calls canonical owners.

Question answered:

> Is the physical seed self-consistent before closed-loop controllers compensate it?

This mode is useful but is **not required for the first M10 diagnostic prototype** if adding the seam would enlarge the blocker fix unnecessarily.

### 8.3 Diagnostic interpretation examples

```text
FIXED_INPUT       PASS
CLOSED_LOOP       DRIFT
```
Likely control/setpoint/controller-memory issue.

```text
FIXED_INPUT       DRIFT
CLOSED_LOOP       DRIFT
```
Likely physical seed/inventory/boundary mismatch.

```text
FIXED_INPUT       DRIFT
CLOSED_LOOP       BOUNDED
```
Controllers are continuously compensating a non-self-consistent authored seed; that may be acceptable operationally only if explicitly qualified, but it is not a physical fixed point.

## 9. Window/trend analysis

One-step residuals alone cannot distinguish a limit cycle from secular drift. The runner should support deterministic sampled windows and record at least:

- current value;
- instantaneous derivative;
- window mean;
- min/max;
- peak-to-peak;
- RMS around mean;
- least-squares signed slope;
- slope repeatability across deterministic reruns.

Initial engineering windows may reuse 60 s / 300 s cadence because those horizons already exist in current evidence, but new pass/fail tolerances must be frozen before promotion, not chosen after seeing a candidate fix.

## 10. Residual ranking

A human-readable report should always show top contributors, e.g.:

```text
TOP EQUILIBRIUM RESIDUAL CONTRIBUTORS
1 outlet.internal-energy-rate
2 drum.mass-rate
3 hotwell.mass-rate
4 header.mass-rate
5 level-control.integral-rate
```

For ranking only, define a machine-readable normalization scale per metric. The normalized score must never hide the underlying physical-unit value.

## 11. Reference Operating-Point Trimmer — later phase only

The trimmer is authorized only after the inspector demonstrates deterministic, interpretable residuals.

### 11.1 Separate three classes of quantities

**Target constraints — fixed by the requested operating point**

Examples:

- desired electrical load;
- rotor/synchronous speed;
- target drum level;
- selected normal control modes;
- selected healthy boundary conditions.

**Trim variables — bounded authoring variables that may be changed**

Candidate families, subject to an explicit allow-list:

- initial node mass inventories;
- initial node internal energies/temperatures within the supported domain;
- steam-drum/hotwell/feedwater inventory allocations;
- initial valve positions/pump commands required for bumpless operation;
- controller integral/bias memory where such state is explicitly owned and physically meaningful;
- initial control-rod position if the target power/reactivity problem requires it.

**Model parameters — forbidden to trim**

- hydraulic resistances;
- heat-transfer coefficients;
- controller gains;
- thermodynamic correlations/envelope limits;
- protection thresholds/delays;
- generator/turbine design constants;
- I.3 budgets or V&V tolerances.

The trimmer finds an operating point for a model. It does not tune the model to a test.

### 11.2 Bounded objective

For a trim vector `z`, the runner evaluates a residual vector `F(z)` through the canonical model. A dimensionless weighted least-squares objective may be used internally:

```text
min || W F(z) ||²
```

subject to hard bounds and hard fail-closed constraints.

Convergence of the optimizer is not qualification. The candidate must still pass the independent equilibrium/stability/trajectory gates.

### 11.3 Algorithm selection

Do not freeze a sophisticated Newton method before the residual/conditioning census exists. Candidate deterministic algorithms:

1. bounded coordinate/bracketing steps for clearly monotonic dominant balances;
2. damped Gauss-Newton / Levenberg-Marquardt using finite-difference Jacobians if the local map is sufficiently regular;
3. deterministic one-sided probes when a centered probe would cross a non-smooth/unsupported boundary.

Randomized/global black-box optimization is not planned.

### 11.4 Versioning rule

A trimmer result that changes a production initial condition creates a **new exact version**. It must never rewrite an already accepted exact identity.

If the current M10 investigation ultimately demonstrates that exact desktop `@4` requires a repaired seed, the default route is:

```text
integrated-operations-desktop-stable@5
```

with exact `@4` preserved unchanged for replay/archive provenance. Any mission/scenario pack whose exact contract intentionally binds `@4` remains unchanged; if the production mission must move to the new seed, it receives its own new exact pack version rather than silently rebinding the old one.

## 12. Stability qualification after trimming

A fixed point can still be unstable. A trimmed reference point is therefore incomplete until deterministic small-perturbation probes demonstrate supported recovery/boundedness.

Candidate perturbation families:

- small drum inventory/level offset;
- small header/steam pressure offset where the authoring seam supports it;
- small load-request change and return;
- small coolant/fuel temperature perturbation in a validation-only seed;
- small controller/actuator offset within normal authority.

Record:

- peak departure;
- return ratio after fixed logical intervals;
- settling time/band;
- residual-vector norm trend;
- protection/fault involvement;
- domain-headroom minimum;
- conservation/coupling safety.

No requirement for monotonic return is imposed unless the model contract specifically requires it.

## 13. Immediate M10 diagnostic subset

If the current long run confirms LR-H1 as a production-semantic failure, implement the smallest diagnostic subset first:

1. test-only/headless exact-v4 residual census;
2. per-node `dm/dt` and `dU/dt` plus 60 s/300 s slopes;
3. controller integral/error/output slopes;
4. canonical conservation/coupling telemetry correlation;
5. sampled `(v,u)` for `outlet` and other top contributors;
6. deterministic rerun equality;
7. no production `src/` change unless the census proves an existing read-only seam is insufficient.

The immediate subset is diagnostic evidence, not the final M12.0 implementation.

## 14. Formal M12.0 work breakdown

M12 should gain a preflight milestone before current M12.1:

### M12.0.1 — Residual taxonomy and participation registry
Freeze metric IDs, ownership, units, participation class and source seams.

### M12.0.2 — Closed-loop Equilibrium Residual Inspector
Implement immutable read-only reports over exact versioned runtime state pairs; no correction.

### M12.0.3 — Domain-headroom and long-trend diagnostics
Add canonical-oracle directional support probes and 60 s/300 s trend classification.

### M12.0.4 — Fixed-input plant-hold qualification seam
Add only the minimum validation-only factory/runner seam needed to separate physical-seed balance from controller compensation.

### M12.0.5 — Bounded Reference Operating-Point Trimmer
Only if the first four slices show a material need. Keep model parameters immutable and trim only allow-listed initial-condition/control-memory variables.

### M12.0.6 — Perturbation stability and closure
Qualify fixed-point/quasi-steady behavior, deterministic repeat, replay provenance implications and long-horizon stability. Close M12.0 before changing M12.1+ physical laws.

M12.0 may legitimately close without a production trimmer if the inspector proves existing authored operating points are adequate and only diagnostics were needed.

## 15. Required artifacts

Planned human-readable evidence:

```text
docs/REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md
docs/EQUILIBRIUM_RESIDUAL_MODEL.md
```

Planned machine-readable contracts/evidence:

```text
eng/reference-operating-point-equilibrium-contract.json
artifacts/reference-operating-point-equilibrium/01-summary.txt
artifacts/reference-operating-point-equilibrium/02-state-residuals.csv
artifacts/reference-operating-point-equilibrium/03-node-mass-energy-residuals.csv
artifacts/reference-operating-point-equilibrium/04-control-residuals.csv
artifacts/reference-operating-point-equilibrium/05-domain-headroom.csv
artifacts/reference-operating-point-equilibrium/06-window-trends.csv
artifacts/reference-operating-point-equilibrium/07-stability-probes.csv
artifacts/reference-operating-point-equilibrium/08-trim-history.csv
```

`08-trim-history.csv` does not exist unless a trimmer is actually authorized.

## 16. Hard non-scope / forbidden shortcuts

The equilibrium work must not:

- change runtime state during normal gameplay;
- insert hidden mass/energy corrections;
- clamp a node before envelope failure;
- create a second plant integrator;
- duplicate thermodynamic/property equations in Application/HMI code;
- widen the 19 I.3 budgets after seeing a failure;
- retune model coefficients merely to reduce residuals;
- weaken protection;
- reinterpret exact historical identities;
- add opaque checkpoint blobs;
- expose engineering residuals as operator truth by default;
- claim industrial steady-state accuracy from internal convergence alone.

## 17. Promotion philosophy

The sequence is intentionally one-way:

```text
observe residuals
→ classify source of drift
→ freeze acceptance
→ if necessary trim a new versioned seed
→ one-step/window equilibrium checks
→ perturbation stability
→ 300 s qualification
→ operational transients
→ cumulative non-regression
→ long soak
```

A long run that remains numerically alive while stationary-required inventories drift secularly is a failure of equilibrium qualification, not a pass.
