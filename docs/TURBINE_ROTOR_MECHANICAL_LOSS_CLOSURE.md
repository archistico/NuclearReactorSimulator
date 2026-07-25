# M10.9.4.1-D.3.1 — Breaker-Open Rotor Mechanical-Loss Closure

## Observed failure

The breaker-open D.2 and D.3 audits produced the same state:

```text
reference             3000 rpm
rotor                  3301.147 rpm
controller output      0%
control valve          0%
effective stage flow   0 kg/s
shaft power            0 MW
saturation             active low
conditional anti-windup active
rotor-speed change     0 rpm
```

The governor was correctly requesting no steam. The rotor nevertheless could not slow because the model contained neither generator load while disconnected nor passive rotational losses.

## Current-v2 law

`TurbineRotorMechanicalLossDefinition` introduces a rated-speed loss-power contract.

For angular speed `ω` and rated speed `ωr`:

```text
Tloss(ω) = (Prated / ωr) × (ω / ωr)
Ploss(ω) = Tloss(ω) × ω
```

Therefore:

- loss torque is linear with speed;
- loss power is quadratic with speed;
- both are zero at rest;
- there is no constant-power singularity near zero speed.

The two sustained current-v2 profiles use:

```text
rated speed       3000 rpm
rated loss power  0.5 MW
```

All legacy/default constructors leave the optional definition absent.

## Why 0.5 MW is a candidate value

The value is deliberately bounded rather than presented as a final reference-plant calibration:

- it is 0.05% of the still-provisional 1000 MWe generator nameplate;
- it is 10% of the validated 5 MWe operating point;
- the D.2 equal-head authority map estimated about 20.9% additional admission capacity between the 28% seed bias and full open, so 0.5 MW does not consume the entire previously measured low-load headroom;
- using the current 1000 kg·m² rotor inertia and the loss law alone, the analytical coast from 3301.147 rpm to the 3150 rpm overspeed reset threshold is about 9.3 s, and to 3005 rpm about 18.6 s. The coupled solver is not required to match these isolated estimates exactly, but must recover within the 90 s audit bound.

The value remains candidate behavior until the closed-breaker 5 MWe journey, the D.2/D.3 audits and Phase-E scale evidence are reviewed together.

## Energy ownership

Passive loss is not generator electrical load.

The turbine solver now distinguishes:

- turbine torque;
- effective external/electromagnetic load torque;
- passive mechanical-loss torque;
- net accelerating torque.

The mechanical audit uses:

```text
ΔErotor = (Pshaft - Pelectromagnetic-load - Ppassive-loss) × Δt
```

The integrated secondary-cycle audit subtracts passive loss from the complete external energy path while preserving the existing mechanical-to-electrical residual between external rotor load and generator mechanical input.

The new diagnostic properties are excluded from replay JSON so historical snapshot serialization is not silently re-versioned.

## Corrected evidence method

The synchronization profile is not assumed stable after an arbitrary five-second delay. The explicit breaker-open audits advance deterministically until all of the following are true:

- breaker remains open;
- any turbine/generator trip caused by the former overspeed transient has first reached its canonical reset-safe conditions and has been explicitly reset;
- no reactor SCRAM is silently cleared by the audit;
- absolute speed error is at most 5 rpm;
- physical control valve is above 0.1%;
- D.3 controller output is not saturated.

Maximum settling allowance: 90 simulated seconds.

Only after this physically controllable, protection-clear baseline is reached does the audit execute the +10 rpm and -10 rpm journey. The reset is not automatic plant behavior: the test issues the same canonical `PROTECTION RESET` command an operator must use after speed is at or below the 3150 rpm overspeed reset threshold and all other reset conditions are safe.

## Scope exclusions

D.3.1 does not change:

- PID gains;
- conditional-integration anti-windup;
- actuator travel rate;
- governor droop;
- turbine hydraulic resistance or Stodola/effective-area law;
- rotor inertia;
- generator nameplate or bidirectional grid coupling;
- protection thresholds;
- timestep or replay schema.
