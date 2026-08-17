# M10.9.4.1-H.6 — Shadow Corrector Rescue Envelope & Two-Tier Qualification

**Status:** VALIDATED

## Purpose

H.5 Hotfix 2 restored production current-v2 to the validated explicit 10 ms coupling and extended the H.4-selected `P060-F040-R015` corrector over 500 committed production intervals in non-authoritative shadow mode. User validation passed build, ordinary tests and the focused shadow gate. The numerical result was intentionally negative for activation: 7/500 intervals triggered correction, only 5/7 converged within 72 iterations at relaxation 0.15, and `extended-shadow-qualification-passes=False`.

H.6 does not change the H.4 trigger envelope and does not activate hybrid production. It characterizes whether the two hard intervals are a bounded Picard-envelope problem (relaxation / iteration budget) rather than a failure of the underlying hydraulic formulation.

## Frozen evidence contract

H.6 freezes the H.5 Hotfix 2 reference trajectory and requires:

- 500 committed explicit current-v2 intervals at 10 ms;
- H.4 trigger thresholds unchanged at predicted subcooled-pressure change `0.060` and predicted hydraulic-flow change `40 kg/s`;
- exactly 7 triggered intervals;
- H.4 primary corrector `R015-I072` converges on 5/7 and does not converge on 2/7;
- production remains `ExplicitCommittedState`;
- shadow candidates never become the start state of the next production interval.

This prevents H.6 from obtaining a false success merely by raising the trigger thresholds and avoiding the difficult states.

## Bounded rescue sweep

H.6 evaluates the same seven committed intervals with six numerical profiles. Physical coefficients, tolerances and trigger thresholds remain fixed; only Picard relaxation and maximum iteration count change:

| Profile | Relaxation | Max iterations |
|---|---:|---:|
| `R015-I096` | 0.150 | 96 |
| `R0125-I096` | 0.125 | 96 |
| `R010-I096` | 0.100 | 96 |
| `R010-I128` | 0.100 | 128 |
| `R0075-I128` | 0.075 | 128 |
| `R0075-I160` | 0.075 | 160 |

The pressure residual tolerance remains `1e-5` relative and the flow residual tolerance remains `1e-2 kg/s`.

Even the largest profile has a bounded deterministic always-use work ratio below 4 over the 500-step qualification window. Wall-clock cost may be recorded but never participates in numerical branching or candidate selection.

## Rescue-profile qualification

A profile is eligible as a rescue only when all seven H.5-triggered intervals converge and all of the following remain true:

- deterministic work ratio <= 4.0;
- maximum relative mass gap versus the committed explicit interval end <= 0.001;
- maximum relative internal-energy gap <= 0.001;
- maximum relative pressure gap <= 0.010;
- inventory integration mass residual <= 1e-6 kg;
- inventory integration energy residual <= 1e-2 J;
- hydraulic mass-rate closure residual <= 1e-8 kg/s;
- hydraulic energy-ownership residual <= 1e-3 W.

Selection among qualifying rescue profiles is deterministic: lowest deterministic work ratio, then lowest maximum iteration count, then ordinal profile ID.

## Two-tier shadow policy

H.6 then evaluates a non-authoritative deterministic two-tier policy:

1. run the H.4 primary `R015-I072` correction on every P060/F040-triggered interval;
2. if the primary converges, retain that shadow candidate for evidence only;
3. if the primary does not converge, discard its candidate and restart from the same committed production state with the selected rescue profile;
4. never commit either candidate to production;
5. never use wall-clock information, hidden damping, coefficient retuning or a changed trigger to decide the result.

The deterministic two-tier work ratio includes both the primary attempt and any rescue retry.

## Interpretation

`refined-envelope-qualification-passes=True` means only that every already-known H.5 difficult interval can be handled by a bounded deterministic primary/rescue envelope while preserving conservation and acceptable local state gaps.

It does **not** activate hybrid production. A positive H.6 result authorizes only a broader scenario/free-running shadow qualification before any future production-activation candidate.

`refined-envelope-qualification-passes=False` means the bounded relaxation/iteration envelope is insufficient and the corrector algorithm itself must be revised before activation is reconsidered.


## Validated result

User validation passed compilation, ordinary `dotnet test` and the focused H.6 gate. The audit preserved 500 committed explicit intervals, P060/F040 and exactly seven trigger events. H.4 primary `R015-I072` remained 5/7 convergent.

The deterministic sweep selected `R0125-I096`, but it converged on only 6/7 events. Reported selected-profile evidence:

- deterministic always-use work ratio `1.438000`;
- maximum pressure residual `0.291876228`;
- maximum flow residual `61.700761261 kg/s`;
- maximum mass/energy/pressure gaps `0.000175566 / 0.000194823 / 0.291958117`;
- `rescue-profile-qualifies=False`.

The two-tier `R015-I072` primary plus `R0125-I096` rescue ladder also converged only 6/7, with deterministic work ratio `1.700000`, exact deterministic repeat and `refined-envelope-qualification-passes=False`. Production remained explicit, no shadow candidate was committed, and no trigger/physical retuning or hidden flow filtering occurred.

This validated negative result closes the bounded fixed-relaxation Picard-envelope question and selects H.7 corrector algorithm revision as the next Phase H step.

## Non-goals

H.6 does not:

- change `PlantNetworkOrchestrator` production routing;
- change the external 10 ms logical timestep;
- alter pipe, valve or pump laws;
- change controller, turbine, generator or protection settings;
- change Phase G enthalpy/shaft-work ownership;
- raise P060/F040 trigger thresholds;
- use adaptive wall-clock behavior;
- add hidden flow filtering or numerical repair;
- close Phase H.
