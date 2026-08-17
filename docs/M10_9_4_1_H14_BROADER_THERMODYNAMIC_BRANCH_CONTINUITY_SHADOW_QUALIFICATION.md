# M10.9.4.1-H.14 — Broader Thermodynamic Branch-Continuity Shadow Qualification

## Baseline

H.14 is built only on the user-validated H.13 Hotfix 2 baseline.

H.13 established that, on the frozen 500-interval P060/F040 evidence set:

- unchanged production H.9 converges 5/7 and exhausts its line search on two events;
- targeted previous-phase continuity converges 7/7;
- targeted bounded previous-phase hysteresis converges 7/7;
- both alternative policies preserve deterministic merit, conservation and ownership safeguards;
- the bounded policy uses 2% relative pressure drift and 5 K temperature drift release limits;
- the H.13 seven-event set exercises no hysteresis release, so continuity and bounded hysteresis are not empirically differentiated there.

Production remains `ExplicitCommittedState` at 10 ms.

## Question

Does the selected bounded previous-phase hysteresis policy remain numerically safe over a materially longer current-v2 shadow horizon, while also proving that its release condition works in both thermodynamic phase directions?

H.14 is qualification, not activation.

## Extended current-v2 shadow window

H.14 reconstructs 2,000 consecutive 10 ms committed production intervals from the same sustained current-v2 runtime used by the H numerical-hardening evidence.

For all 2,000 intervals it:

- verifies production remains explicit and trip-free;
- reconstructs the frozen non-hydraulic balances;
- re-evaluates the frozen P060/F040 trigger;
- preserves the first 500 intervals as an H.13 control window, which must still contain exactly seven trigger events with H.4 primary 5/7;
- runs the unchanged H.9 Jacobian corrector plus targeted H.13 bounded hysteresis at every trigger found across the full 2,000-interval horizon.

The policy remains restricted to `steam` and `stop-out`. H.14 does not generalize it plant-wide.

## Committed-state observation

At every committed interval, H.14 independently re-resolves `steam` and `stop-out` through the unchanged production resolver and the bounded shadow wrapper. This produces 4,000 target-node observations and records:

- committed phase;
- production re-resolve phase;
- shadow-selected phase;
- branch override;
- previous-phase hold;
- hysteresis release;
- pressure/temperature drift;
- committed phase transitions over the extended horizon.

These observations are never committed.

## Explicit hold/release challenges

Because the H.13 frozen set exercised zero release events, H.14 includes four deterministic boundary-selection challenges using the concrete H.11/H.12 `steam` and `stop-out` inventories:

1. `steam` near-boundary case — production jumps superheated; bounded hysteresis must hold `SaturatedMixture`;
2. `steam` deliberately out-of-band previous state — bounded hysteresis must release to production `SuperheatedVapor`;
3. `stop-out` near-boundary case — production jumps saturated; bounded hysteresis must hold `SuperheatedVapor`;
4. `stop-out` deliberately out-of-band previous state — bounded hysteresis must release to production `SaturatedMixture`.

This checks both hold and release semantics in both phase directions. The deliberately distant previous states are diagnostic qualification challenges, not production state edits.

## Qualification

The selected policy qualifies only if:

- the H.13 500-interval control window is reproduced;
- every P060/F040 event found in the 2,000-interval horizon converges under unchanged H.9 tolerances;
- no H.9 line search exhausts on those events;
- accepted H.9 merit remains strictly decreasing;
- exact deterministic repeat holds;
- deterministic work stays inside the existing H.9 audit limit;
- mass closure and hydraulic energy ownership remain inside existing tolerances;
- committed-state target observation is exactly deterministic;
- all four hold/release challenges pass exactly and deterministically.

`broader-shadow-qualification-passes=False` does not by itself fail the H.14 audit. It means H.14 does not authorize an activation candidate.

## Production isolation

H.14 does not:

- modify `SimplifiedWaterSteamThermodynamicModel.Resolve()`;
- introduce previous-state hysteresis into production;
- modify `ThermodynamicBranchContinuityModel` from validated H.13;
- modify H.3-H.9 hydraulic correctors;
- modify `PlantNetworkOrchestrator` routing;
- retune P060/F040;
- alter physical coefficients or H.9 residual tolerances;
- change the 10 ms production timestep;
- commit a shadow candidate;
- introduce active-set, semi-smooth or thermodynamic clamp behavior.

## Decision after H.14

If broader qualification passes, the next step may design a production-isolated activation candidate with explicit rollback, shadow comparison and strict release monitoring. Passing H.14 alone does not activate hysteresis or the H.9 corrector.

If broader qualification fails, production stays explicit and the extended trigger/committed-state/release evidence determines whether the policy limits need further diagnosis or the targeted continuity approach must remain shadow-only.
