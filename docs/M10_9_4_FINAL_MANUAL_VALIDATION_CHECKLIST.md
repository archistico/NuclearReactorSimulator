# M10.9.4 Final Manual HMI / Engineering-Schematic Validation Checklist

**Status:** PASSED — user-confirmed on 2026-07-24. M10.9.4 is the official validated baseline.

## Purpose

Hotfix 23 has passed compilation, the complete ordinary suite and both explicit 60-second gameplay journeys. This checklist is the final evidence required to promote M10.9.4 from automated-green checkpoint to validated milestone.

Record each item as PASS / FAIL with a short note or screenshot reference where useful.

## Environment

- [x] Clean Hotfix 23 build is being tested.
- [x] Default desktop scenario loads without startup error.
- [x] Minimum supported window size and normal maximized layout are both checked.

## Engineering schematics

- [x] Reactor workspace shows the rod → reactivity → neutron/power → thermal/void feedback chain clearly.
- [x] Primary workspace shows drum → suction → MCP → header → channels → return plus steam export/feedwater.
- [x] Turbine workspace shows main steam → stop/control/admission valves → turbine → shaft → exhaust → condenser/feedwater.
- [x] Grid workspace shows shaft → generator → breaker → grid and separate synchronization/protection signal paths.
- [x] Instrumentation/protection workspace uses signal-flow grammar distinct from piping and makes protection priority unambiguous.

## Readability and semantics

- [x] No schematic overlaps, clipping or unreadable labels at minimum size.
- [x] Every process path has unambiguous direction and endpoints.
- [x] Equipment cards expose meaningful IN/OUT information.
- [x] Amber SHAFT is clearly understood as mechanical-energy medium, not warning severity.
- [x] Measured / Model Diagnostic / Unavailable provenance remains visible and is not silently merged.

## Generator power-path diagnostics

- [x] Breaker open: 0 MWe is explained as expected and the next synchronization/close action is clear.
- [x] Breaker closed with zero requested load: LOAD RAISE is identified as the missing action.
- [x] Requested load with insufficient shaft support: diagnostics direct attention to steam/admission/turbine/protection rather than suggesting more load.
- [x] Requested MWe, actual MWe, turbine shaft power, generator mechanical input, speed/frequency, breaker and trip state are distinguishable.

## Regression smoke check

- [x] Existing gauges, whole-plant mimic and subsystem selection still work.
- [x] Operator Computer F1–F8 navigation and keyboard operation still work.
- [x] RUN / PAUSE / SINGLE STEP remain synchronized.
- [x] Alarm acknowledgement/reset and protection reset remain separate canonical actions.
- [x] Checkpoint/replay/session functionality remains accessible and coherent.

## Sign-off

- [x] **M10.9.4 manual acceptance passed.**

Promotion completed: the authoritative project documents record M10.9.4 as validated. The later M10.9.4.1-A audit has since executed and found a ~70-second trip; that follow-on result does not reopen this completed manual HMI acceptance.
