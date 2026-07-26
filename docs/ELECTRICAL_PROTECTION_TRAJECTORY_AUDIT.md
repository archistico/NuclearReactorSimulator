# Electrical Protection Trajectory Audit

## Status

**M10.9.4.1-E.3.1 Hotfix 1 — IMPLEMENTED CANDIDATE / LOCAL AUDIT PENDING**

The validated parent is **M10.9.4.1-E.2 Hotfix 1**. E.3.1 deliberately adds no protection thresholds and no new trip action. It records deterministic signed electrical trajectories so the later E.3.2 relay definitions can be derived from observed current-v2 behavior rather than arbitrary constants.

## Why this phase is separate

The validated E.2 coupling can represent:

- positive generator export;
- negative import/motoring;
- breaker-open rotor coastdown;
- breaker-closed frequency slip and electrical-angle restoring behavior.

Those states are necessary but not sufficient to select reverse-power, underfrequency or loss-of-synchronism thresholds. A protection threshold must remain clear of normal/load-step trajectories, must not trip a disconnected machine, and must respond deterministically to the intended abnormal state.

## Audit scenarios

The explicit `ElectricalProtectionTrajectoryAudit` pack contains four tests.

### 1. Normal breaker-closed generation and load step

The sustained desktop profile runs through:

```text
5 MWe request → 0 MWe request → 5 MWe request
```

The audit records normal power, frequency-slip and phase-angle envelopes while the breaker remains closed.

### 2. Turbine trip with breaker still closed

The sustained desktop profile receives a manual turbine trip and, in the same command boundary, its requested electrical power is lowered from 5 MWe to zero without issuing a generator trip. The breaker therefore remains closed and the bidirectional coupling may motor the shaft. This is the primary reverse-power calibration trajectory.

### 3. Breaker-open coastdown

The synchronization profile begins with the breaker open. A turbine trip removes steam torque and the rotor coasts down. Falling frequency in this state is intentionally **not eligible** for a generator underfrequency trip. The trajectory therefore defines the required breaker/load-state supervision boundary.

### 4. Breaker-closed phase-offset sweep

The validated current-v2 physical state is reused with deterministic initial generator/grid angle offsets:

```text
-135°, -90°, -45°, -15°, +15°, +45°, +90°, +135°
```

The low-level generator/grid solver records signed phase lead, absolute phase separation, frequency slip, exchange power and phase-wrap count over five simulated seconds. This is evidence for the reduced-order coupling's restoring envelope; it is not a claim of detailed synchronous-machine transient fidelity.

## Output

Run:

```text
scripts\run-electrical-protection-trajectory-audit.cmd
```

The script clears and recreates:

```text
artifacts\e3-protection-trajectories
```

It then prints the summary files and retains detailed CSV trajectories. Runtime reports include requested power, signed grid exchange, mechanical exchange, conversion loss, electrical frequency, frequency slip, absolute and signed phase separation, breaker state and trip state. The summary and CSV data must be reviewed before E.3.2 is designed.

## Evidence required for E.3.2

The next phase must derive and document:

- reverse-power pickup, reset level and intentional delay from the normal/load-step and turbine-trip envelopes;
- explicit breaker-closed supervision for underfrequency;
- any additional load or turbine-state supervision required to prevent nuisance operation;
- loss-of-synchronism observables supported by the reduced-order model;
- angle/slip pickup and delay clear of all restoring phase-offset cases;
- reset and latching semantics;
- replay/checkpoint determinism across pickup, timer accumulation, trip and reset.

No threshold is approved merely because it is common in real plants. The simulator uses an educational reduced-order 10 MWe current-v2 model, so the active thresholds must be grounded in its validated trajectories and clearly documented as non-licensing values.
