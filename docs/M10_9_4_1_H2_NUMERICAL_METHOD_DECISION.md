# M10.9.4.1-H.2 — Deterministic Semi-Implicit Pressure/Flow Method Decision

**Status:** VALIDATED — user-confirmed compilation, ordinary tests and H.1 evidence reproduction passed; no runtime integration change is active in H.2.

## 1. Validated evidence basis

H.1 is user-validated. Its fixed-step audit compared the same current-v2 desktop operating point at 10, 5 and 2.5 ms while preserving the same 20 ms deterministic seed-preconditioning duration.

Reported evidence:

```text
steps                              10 / 5 / 2.5 ms
wall seconds per simulated second  0.824089 / 1.592215 / 3.188456
cost ratio 5 ms / 10 ms            1.932092
cost ratio 2.5 ms / 5 ms           2.002528
max pump flow one-step change       23.483512764 kg/s
max channel flow one-step change    114.314039863 kg/s
max return flow one-step change     86.409873575 kg/s
max fractional mass change/step     0.001965068
max fractional energy change/step   0.002079844
max liquid pressure change/step     0.088657847
max final relative diff 10→5 ms     0.005401937
max final relative diff 5→2.5 ms    0.006028534
refinement improves                 False
```

Conservation remained green, so the evidence indicates a pressure/flow coupling stiffness problem rather than inventory divergence.

## 2. Decision

H.2 explicitly selects **a deterministic semi-implicit pressure/flow coupling** for further implementation and validation.

The other two H.1 decision branches are rejected as the preferred final cure:

1. **Retain explicit 10 ms unchanged:** rejected as the final numerical-hardening answer because large raw hydraulic step changes remain and refinement does not show monotonic improvement. The 10 ms step remains the production baseline until the selected replacement is proven.
2. **Bounded explicit substeps:** rejected as the preferred cure because halving the step approximately doubles runtime cost while the maximum final-state relative difference does not improve from 10→5 to 5→2.5 ms.
3. **Semi-implicit pressure/flow coupling:** selected because the dominant debt is the algebraic pressure/flow feedback itself, not simply insufficient explicit temporal resolution.

This is a numerical-method decision, not a physical retuning.

## 3. Invariants that the selected method must preserve

The future implementation must preserve all of the following:

- external logical timestep remains deterministic and version-owned;
- wall-clock performance never changes the simulation path;
- components read one canonical committed state at the logical-step boundary;
- conserved mass and internal energy remain integrated exactly once per logical step;
- no second owner for inventory integration is introduced;
- pipe, valve and pump physical laws remain unchanged;
- reverse-flow semantics remain component-owned;
- pump hydraulic work and shaft demand remain single-owned;
- current-v2 enthalpy transport ownership from Phase G remains unchanged;
- source terms, relief, bypass, turbine work and external exchanges remain single-owned;
- replay/checkpoint determinism must remain exact;
- legacy/current-v1 behavior remains unchanged unless explicitly version-migrated;
- no hidden damping, flow filtering, coefficient retuning or topology-changing interlock may be introduced merely to stabilize the solver.

## 4. Selected coupling concept

The semi-implicit treatment will target the nonlinear loop:

```text
node pressure
    ↓
pipe / valve / pump flow
    ↓
node mass + advected energy balance
    ↓
thermodynamic closure
    ↓
next node pressure
```

The current explicit solver evaluates all hydraulic flows from committed pressures and then integrates once. The selected future method will solve the pressure/flow relation within the same logical step using a bounded, deterministic nonlinear iteration or equivalent coupled treatment, while still committing conserved inventories only once.

H.2 does **not** freeze a particular iteration count, relaxation factor or convergence tolerance. Those parameters must be derived and audited in H.3 rather than guessed into production.

## 5. Staged implementation

### H.3 — Isolated Semi-Implicit Hydraulic Prototype & Audit

H.3 will implement the selected method behind an explicit non-production/audit seam first. It must compare explicit 10 ms against the prototype on the same current-v2 point and record at minimum:

- primary pump/channel/return one-step flow changes;
- dominant subcooled-liquid pressure one-step changes;
- final-state difference against a defined reference;
- iteration/convergence residuals;
- mass/energy closure;
- deterministic replay equivalence;
- runtime cost per simulated second;
- bounded behavior under representative off-design hydraulic states.

### H.4 — Current-v2 Activation Gate

Only if H.3 shows a material numerical improvement without violating ownership, determinism, conservation or bounded cost may H.4 activate the method for current-v2. H.4 must then rerun all ordinary, explicit long-running, protection, F/G ownership and operational-envelope gates.

If H.3 fails, the method is redesigned; H.4 is not reached by weakening tolerances.

## 6. Production state in H.2

```text
production-fixed-step                10.000 ms
production-pressure-flow-coupling    explicit committed-state
semi-implicit-runtime-active         False
bounded-explicit-substeps-selected   False
wall-clock-adaptation-active          False
physical-retuning-active             False
H.3-prototype-required                True
H.4-activation-gate-required          True
```

H.2 therefore changes architecture direction and documentation only. It does not claim that the stiffness problem is already solved.
