# M10.9.4.1-D.4 Validation Record

## Status

**VALIDATED — 2026-07-25**

D.4 adds operator-facing turbine valve authority without moving plant physics into Avalonia. The validated baseline is cumulative through D.3.2 Hotfix 3 and D.4.

## Implemented behavior

- typed STOP and ADMISSION valve OPEN/CLOSE commands;
- control-valve AUTO / MANUAL authority selection;
- bounded 0–100% manual-demand slider with explicit APPLY;
- no command dispatch while the slider is merely moved;
- requested/manual-demand/actual positions published separately;
- finite actuator travel preserved;
- protection remains later authority and can force the stop valve closed without erasing the operator request;
- manual demand is rejected until MANUAL mode is active.

## Automated validation evidence

Ordinary suite:

- total discovered: **961**;
- passed in the ordinary run: **944**;
- failed: **0**;
- explicit/opt-in skipped: **17**.

All 17 unique explicit tests were then executed and passed:

| Explicit gate | Result |
|---|---:|
| Turbine admission authority | 3/3 |
| Governor/actuator tracking | 2/2 |
| Gameplay long-running journeys | 2/2 |
| Operational-envelope audit | 9/9 |
| Reference-plant scale audit | 2/2 |

The script totals include one overlap between the operational-envelope and reference-scale categories; therefore 18 script test executions correspond to **17 unique explicit tests**.

## D.4 focused regression coverage

Application tests verify:

- MANUAL demand publishes requested and actual positions separately;
- AUTO returns authority to the governor;
- an OPEN request remains visible while turbine trip forces actual STOP closed;
- admission-valve CLOSE uses normal actuator travel;
- manual demand is rejected outside MANUAL mode.

App tests verify:

- slider movement alone dispatches nothing;
- APPLY emits a bounded typed manual-demand command;
- STOP/ADMISSION buttons target canonical valve identifiers;
- XAML contains the 0–100 slider and explicit APPLY command.

## Remaining non-blocking hardening

D.4.1 may add dedicated replay/checkpoint regressions, trip-reset-resume coverage, stop-valve-owned travel-rate configuration and a manual TURBINE-station usability pass. These are follow-up hardening items, not failures of the validated D.4 automated gate.
