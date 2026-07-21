# ADR 0063 — Secondary-system transients perturb canonical inputs and protection seams

**Status:** Accepted for M8.4 baseline candidate

## Context

M8.1 provides deterministic fault scheduling, M8.2 hydraulic component effects and M8.3 instrumentation/control faults. M8.4 must provide turbine, generator, feedwater and condenser transients without creating duplicate rotor, electrical, thermofluid or protection state owners.

## Decision

1. Turbine-trip and generator-trip scenario events are one-shot activations of the already validated M5.5 manual trip inputs. M8.4 does not set stop-valve position, breaker state, rotor speed or electrical power directly.
2. Trip latches remain canonical M5.5 logical state. Clearing the M8.4 scenario fault lifecycle does not reset a protection latch; only the existing permissive-gated protection reset path may do so.
3. Feedwater loss/degradation reuses M8.2 `hydraulic.pump-trip` and `hydraulic.pump-degradation` against the canonical feedwater pump. M8.4 does not introduce a second feedwater flow or inventory integrator.
4. Condenser degradation/loss acts only by replacing the per-step M4.3 cooling-boundary capacity input with a deterministic fraction of the persistent baseline. The condenser solver remains the sole owner of condensation flow, heat rejection and derived pressure/vacuum consequences.
5. M8.4 uses a versioned transient-ready initial condition derived from the existing M7 recipe. The only added baseline input is finite condenser heat-rejection capacity; no authoritative state is patched after construction.
6. Scenario definitions remain explicit immutable data with exact logical-step triggers. No transient outcome is scripted or corrected to match an expected curve.
7. Target IDs bind fail closed to canonical rotor, generator, feedwater-pump and condenser-cooling-boundary IDs.

## Consequences

- load rejection emerges from the existing generator-trip/breaker/electromagnetic loading chain;
- turbine rundown/steam isolation emerge from existing M4/M5 mechanics and protection;
- feedwater inventory response remains conservative under the single plant-network orchestration boundary;
- condenser pressure/vacuum remain derived physical diagnostics rather than synthetic scenario state;
- later M8.5/M8.6 scenarios can compose these transient families without bypassing validated ownership.
