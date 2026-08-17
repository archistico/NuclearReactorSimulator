# M10.9.4.1-H.3 — Isolated Semi-Implicit Hydraulic Prototype & Audit

**Status:** HOTFIX 1 VALIDATED — prototype/audit only; production current-v2 remains on the validated explicit 10 ms pressure/flow path.

User validation: compilation, ordinary `dotnet test` and the focused H.3 audit passed. The 50/50 intervals converged with 16.760 average / 40 maximum iterations. Prototype/explicit chatter ratios were pump 0.432808, channel 0.135868 and return 0.031681; pressure ratio was 0.922127. Conservation/ownership residuals were zero and deterministic repeat was exact. Full-time prototype cost was approximately 15.894951x the isolated explicit replay, so production activation remained deferred to H.4.

## 1. Validated prerequisite

H.2 is user-validated. H.1 evidence showed that explicit refinement from 10 to 5 to 2.5 ms approximately doubles cost at each halving without monotonic final-state convergence improvement. H.2 therefore selected deterministic semi-implicit pressure/flow coupling as the next method to prototype.

H.3 does not activate that method in production.

## 2. Prototype ownership

H.3 adds `SemiImplicitHydraulicPrototypeSolver` under `Simulation.Plant` as an explicit audit-only seam. It reuses the existing component laws without changing coefficients:

- `PipeFlowSolver`;
- `ValveFlowSolver`;
- `PumpFlowSolver`;
- `FluidNodeIntegrator`;
- current Phase G energy-transport modes.

The prototype performs bounded deterministic Picard iteration over the same logical timestep. Pipe/valve/pump flows are reevaluated against provisional end-of-step fluid states. The numerical iterate is under-relaxed, but physical resistances, pump boosts, valve characteristics and state ownership are never filtered or retuned.

Each provisional candidate is always reconstructed from the original committed inventory plus one logical-step balance. No provisional iteration is committed and no second inventory integrator is introduced.

## 3. Frozen-forcing comparison

The H.3 current-v2 audit reconstructs 0.5 s of the validated 10 ms desktop trajectory. For every reference interval it derives the total fluid-node mass/internal-energy rate directly from the reference inventory delta, subtracts the instantaneous hydraulic contribution and freezes the remainder as the non-hydraulic forcing for that interval.

This frozen remainder includes whatever the validated full-plant step owned outside the isolated pipe/valve/pump coupling, for example heat transfer, drum/source terms, condenser/turbine/boundary contributions and other staged balances.

The audit then runs two isolated replays with identical forcing:

1. one-pass explicit hydraulic evaluation;
2. the H.3 semi-implicit prototype.

The explicit replay must reproduce the reference inventories. The semi-implicit replay therefore changes only the pressure/flow coupling treatment.

## 4. Deterministic iteration contract

Default audit controls are:

```text
maximum iterations             96
under-relaxation factor        0.10
relative pressure tolerance    1e-5
absolute flow tolerance        1e-2 kg/s
```

These are H.3 prototype parameters, not accepted production constants. H.4 may not inherit them automatically.

Iteration termination depends only on deterministic simulation values. Wall-clock timing is diagnostic only.

## 5. Evidence recorded

The explicit/prototype comparison records:

- main-circulation pump one-step flow change;
- channel one-step flow change;
- return one-step flow change;
- maximum fractional subcooled-liquid pressure change;
- convergence count and maximum iteration count;
- maximum pressure and flow residuals;
- hydraulic mass-rate closure;
- pump hydraulic-energy ownership residual;
- exact inventory-integration residual;
- final mass/internal-energy/pressure gap against the explicit reference;
- deterministic repeat equivalence;
- wall-time overhead versus the isolated one-pass explicit replay.

The validated audit reports material chatter/pressure improvement but also an unacceptable approximately 15.895x full-time cost. H.3 therefore remains evidence for the method, not a production activation. H.4 owns the bounded-cost hybrid gate.

## 6. Off-design evidence

Ordinary Simulation tests use a deliberately stiff two-node compressible-liquid surrogate. They verify that:

- the one-pass explicit update can overshoot;
- the semi-implicit fixed-point treatment converges and reduces that overshoot;
- reverse-flow semantics remain intact;
- hydraulic mass/energy ownership closes;
- repeated identical runs are bitwise deterministic at the recorded double values;
- invalid source targets are rejected explicitly.

This is numerical evidence only, not a replacement thermodynamic model.

## 7. Production invariants

H.3 must leave all of the following unchanged:

```text
production logical timestep          10 ms
production pressure/flow method      explicit committed-state
semi-implicit production active      False
adaptive timestep active             False
physical coefficient retuning        False
hidden flow filtering                False
wall-clock adaptation                False
legacy/current-v1 behavior           unchanged
```

The approved future accident/core/control-room backlog remains outside Phase H implementation scope.
