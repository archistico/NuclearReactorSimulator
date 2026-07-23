# ADR 0091 — Current-v2 condenser preserves the UA design point with installed-capacity headroom

## Status

Accepted as an M10.9.4.1-A.2 Hotfix 1 implementation candidate; local validation pending.

## Context

The validated M10.9.4 current-v2 condenser uses:

- cooling water at 20 °C;
- overall heat-transfer conductance `UA = 1.225 MW/K`;
- an initial steam-space temperature near 40 °C;
- available cooling-boundary power of 24.5 MW;
- maximum condensation flow of 15 kg/s.

At the initial point, `UA * ΔT` is also 24.5 MW. Because the solver uses
`min(Q_available, UA * ΔT)`, the installed-capacity ceiling clips the pressure-feedback law exactly at the design point. When exhaust pressure and saturation temperature rise, the surface law can calculate more heat transfer, but the 24.5 MW boundary prevents that additional rejection from becoming effective.

The 300-second current-v2 healthy-reference audit repeatedly latched `TurbineTrip | GeneratorTrip` near 70 simulated seconds. Rotor speed and generator frequency remained far below their trip thresholds, so the action signature identifies the `condenser-high-backpressure` function crossing its unchanged 30 kPa threshold between ten-second observations.

## Decision

For the current-v2 sustained-generation and sustained-synchronization seeds only:

- preserve cooling-water temperature at 20 °C;
- preserve `UA = 1.225 MW/K`;
- preserve the 40 °C initial surface-transfer point at 24.5 MW;
- raise installed cooling-boundary capacity from 24.5 MW to 40 MW;
- raise maximum condensation flow from 15 kg/s to 20 kg/s;
- sample the extended audit once per simulated second and publish exact latched-function, stage-flow, condenser-limit and exhaust-mass evidence.

No condenser solver equation, protection threshold/action, timestep, controller, replay schema or legacy/v1 seed changes.

## Consequences

- Initial current-v2 condenser heat rejection remains governed by the same 24.5 MW `UA * ΔT` value.
- Above the initial point, the existing negative feedback may increase heat rejection until the 40 MW installed ceiling is reached.
- The hard mass-flow ceiling has explicit margin over the approximately 15 kg/s turbine path.
- The candidate remains invalid until build, ordinary tests, both 60-second journeys and the complete 300-second audit pack pass locally.
- This decision does not resolve condensate/hotwell energy fidelity, reference-plant scale, turbine admission authority or broader condenser phase-change closure.
