# ADR 0099 — Current-v2 turbine admission is vapor-mass-fraction limited

## Status

Proposed / M10.9.4.1-D.1 candidate.

## Context

The pressure-driven current-v2 stage-flow law could request positive mass flow whenever inlet pressure exceeded exhaust pressure, even when the turbine inlet state was subcooled liquid or a wet mixture. The thermodynamic-work law separately reduced available work with vapor mass fraction. That combination allowed liquid mass to cross the turbine stage as a zero-work bypass.

## Decision

`TurbineStageGroupDefinition` owns an explicit versioned `TurbineAdmissionPhasePolicy`. The default is `LegacyUnrestricted` so historical definitions and replay-compatible profiles retain their prior semantics. Sustained current-v2 profiles explicitly select `VaporMassFractionLimited`.

Under `VaporMassFractionLimited`:

- the hydraulic stage request remains the commanded/raw request;
- effective stage transfer is multiplied by the committed turbine-inlet vapor mass fraction;
- subcooled liquid therefore admits zero stage mass flow;
- a saturated mixture admits only its vapor mass fraction;
- thermodynamic specific work is evaluated per kilogram of admitted vapor, so vapor quality is not multiplied a second time;
- mass/energy source terms continue to use the single effective stage flow and remain integrated once.

## Consequences

This closes the silent liquid-bypass defect without choosing a detailed wet-steam erosion/separation model. It does not yet prove that the current valve/stage resistance distribution gives sufficient governor authority. D.2 must measure control-valve position versus inlet pressure, stage flow and shaft power before selecting resistance rescaling, effective-area control or a Stodola/ellipse-style law.

No global enthalpy/flow-work migration is implied; that remains Phase G.
