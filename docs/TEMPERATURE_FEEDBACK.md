# Temperature Reactivity Feedback

M2.6 introduces the first closed physical feedback path between the thermal state of the plant and neutron kinetics.

## Model boundary

Temperature feedback is deliberately algebraic and stateless:

```text
committed temperature T
        - reference temperature T_ref
        ↓
ΔT
        × temperature coefficient α_T
        ↓
named ReactivityContribution
        ↓
ReactivityModel
        ↓
PointKineticsSolver
```

The current linear law is:

`rho_T = alpha_T * (T - T_ref)`

`TemperatureReactivityCoefficient` is stored canonically as `delta-k/k per kelvin` and exposes explicit `pcm/K` conversion. The coefficient is signed. The generic engine does not assume that fuel or coolant coefficients must be negative or positive; plant configuration owns those choices.

## Supported sources

A `TemperatureReactivityFeedbackDefinition` is explicitly categorized as either:

- `FuelTemperature`; or
- `CoolantTemperature`.

Each definition has a globally meaningful contribution ID, a reference temperature and a coefficient. Multiple fuel zones or coolant regions can therefore contribute independently and remain visible in the normal M2.1 reactivity breakdown.

## Deterministic coupling

M2.6 uses temperatures from the committed state at the beginning of each fixed timestep. The generated reactivity contribution is then used to advance neutron kinetics. Fission/decay heat subsequently changes thermal inventories for the candidate next state.

This creates an explicit one-fixed-step coupling lag rather than a hidden algebraic iteration:

```text
committed fuel/coolant temperature
        ↓
temperature reactivity
        ↓
neutron kinetics
        ↓
fission + decay heat
        ↓
thermal/fluid integrators
        ↓
next committed temperature
```

The choice keeps the kernel deterministic, replayable and transactionally compatible with M0.3. A future higher-fidelity coupled solver may replace this staging behind explicit interfaces if required.

## Non-goals

M2.6 does not introduce:

- hardcoded RBMK temperature coefficients;
- nonlinear Doppler tables;
- burnup-dependent coefficients;
- spatial flux/temperature coupling;
- void feedback (M2.7);
- iodine/xenon dynamics (M2.8).
