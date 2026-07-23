# ADR 0079 — Generation-ready operating seeds are versioned; effective turbine steam flow is presentation-only

## Status

Accepted for M10.9.4 Hotfix 6 implementation candidate.

## Context

The first successfully executed explicit long-gameplay gate exposed a real integrated power-path defect in the historical desktop operating seed:

- at 10 simulated seconds the generator breaker remained closed and 5 MWe remained requested, but output had fallen to about 2.406 MWe;
- rotor speed had decayed to about 1442.6 rpm;
- MODEL rotor shaft power was 0 MW;
- the synchronization/load journey separately drove `control-out` outside the supported simplified water/steam state envelope.

Code review showed that the historical `integrated-operations-desktop-stable` v1 seed was created as an M9.7 numerical-stability/GUI-runtime seed, not as a sustained thermodynamic/electrical operating point. Its 120 °C near-isobaric steam path, high admission resistance and proportional-only speed controller did not provide a persistent mechanical-power balance for a 5 MWe electrical request. The old presentation field `TotalSteamFlow` also represents the legacy M4.1 turbine-admission boundary seam, which remains zero while M5.4 derives actual turbine-stage mass flow from the commanded canonical stop/control/admission valve path.

Changing v1 in place would invalidate exact-version replay/archive identity.

## Decision

1. Historical versioned initial conditions are immutable once used as replay/archive origins. `integrated-operations-desktop-stable` v1 and `pre-synchronization-grid-loading` v1 remain registered and unchanged.
2. New generation-ready behavior is introduced through v2 initial-condition factories. The current desktop integrated-operations program points to `integrated-operations-desktop-stable` v2; the explicit synchronization journey uses `pre-synchronization-grid-loading` v2 while the historical M7.5 scenario remains v1.
3. The v2 operating recipes establish a staged pressurized steam path, admission hydraulic capacity, bumpless PI governor bias, condenser capacity/heat rejection and condensate/feedwater pump capacity/bias appropriate to the low-load operating point. These are deterministic initial-condition parameters; they do not move M4/M5 ownership into Application presentation or Avalonia.
4. The v2 instrumentation definition explicitly publishes `plant/turbine/total-shaft-power`, so aggregate turbine shaft power can remain a true MEASURED presentation channel rather than falling back to MODEL state.
5. `TurbineSecondaryPanelSnapshot.EffectiveTurbineSteamFlow` exposes the actual model-derived sum of turbine stage-group effective mass flows for HMI/diagnostics. It is `[JsonIgnore]` presentation metadata so fingerprint-v1 keeps the historical serialized `TotalSteamFlow` field unchanged.
6. UI and diagnostics must use effective stage flow when explaining turbine admission, while historical post-incident/fingerprint fields remain untouched unless separately versioned.
7. The explicit 60-second gameplay gate remains authoritative for promotion. Candidate tuning is not considered validated until both the ordinary suite and explicit long-gameplay journeys pass locally.

## Consequences

- old recordings and archives can still resolve their exact v1 initial-condition factories;
- new desktop sessions can start from a physically supported low-load generation candidate without rewriting historical identity;
- the HMI no longer reports a misleading `0 kg/s` steam-flow seam when turbine stages are actually flowing;
- measured-vs-model provenance remains explicit;
- future operating-point changes require a new initial-condition version instead of silently mutating a replay origin;
- M10.9.4 remains a candidate until the user validates the ordinary and explicit long-running gates.
