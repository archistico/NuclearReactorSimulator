# ADR 0102 — Current-v2 breaker-open rotor has passive mechanical losses

## Status

Proposed / M10.9.4.1-D.3.1 candidate.

## Context

The corrected D.2/D.3 breaker-open audits exposed a deterministic dead state. After five simulated seconds the rotor was at about 3301 rpm while the speed reference remained 3000 rpm. The controller was correctly saturated low, conditional anti-windup was active, the physical control valve was fully closed, effective stage flow and shaft power were zero, yet rotor speed remained exactly constant.

The cause was not missing governor authority. With the generator breaker open, the electrical solver commanded zero electromagnetic torque. The rotor model also contained no bearing, windage or uncoupled-generator loss torque. Once turbine torque reached zero, net torque was therefore zero and the rotor could not decelerate. Waiting or lowering the steam command could never restore synchronization speed.

The evidence also corrects the D.3 Hotfix 1 interpretation: the breaker-open synchronization profile uses P=0.5, I=0.02 s⁻¹ and D=0. The decisive failure was not a missed derivative kick; it was the absence of a physical braking path.

## Decision

Add an optional `TurbineRotorMechanicalLossDefinition` to the rotor definition.

The loss law is deliberately simple and non-singular:

```text
loss torque ∝ angular speed
loss power  ∝ angular speed²
loss torque = 0 at rest
```

`RatedSpeedLossPower` owns the design value. The solver resolves the passive loss torque from committed rotor speed, keeps it distinct from generator electromagnetic load, includes it in the rotor net-torque equation and closes the mechanical and full secondary-cycle energy audits explicitly.

Only the sustained current-v2 desktop and synchronization seeds opt in, with 0.5 MW loss power at 3000 rpm. Historical and other profiles retain `MechanicalLoss = null` and therefore preserve their previous behavior.

The 0.5 MW value is a bounded calibration candidate, not a final plant-scale claim: it is 0.05% of the provisional 1000 MWe nameplate, 10% of the validated 5 MWe point and below the roughly 1.1 MW equal-head admission headroom inferred by D.2 from 28% to full-open. With the current 1000 kg·m² inertia, the isolated loss law predicts approximately 9.3 s from 3301.147 rpm to the 3150 rpm reset threshold and 18.6 s to 3005 rpm. Coupled validation, not these isolated calculations, remains authoritative.

The explicit breaker-open audits must first wait for the rotor to decelerate. If the historical transient has latched the canonical overspeed turbine/generator trip, the audit waits until every protection reset condition is safe, issues the canonical `PROTECTION RESET` command, verifies that it is accepted, and only then requires a controllable band within ±5 rpm of the effective setpoint. A reactor SCRAM is never silently reset by this helper. The existing +10/-10 rpm journey begins only from that protection-clear state. This is a physics and recoverability gate, not a fixed-delay workaround: failure to recover within 90 simulated seconds remains a test failure.

## Consequences

- An open-breaker rotor can now coast down when steam admission closes.
- Re-synchronization after load rejection can become physically possible without inventing reverse generator torque; any latched overspeed protection must still be reset explicitly after its canonical reset-safe conditions are met.
- Generator electromagnetic torque and passive mechanical loss remain separate quantities.
- Mechanical and full reactor-to-grid energy closure include the dissipated power.
- The current-v2 0.5 MW value consumes some low-load turbine headroom and must be reviewed together with the Phase-E scale contract.
- No new tracking anti-windup law is introduced in D.3.1. D.3 evidence is rerun after the deceleration path exists; only then can integral recovery be classified correctly.
