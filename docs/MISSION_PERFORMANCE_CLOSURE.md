# Mission / Performance closure

M10.9.7.5 Hotfix 1 is the active cumulative closure gate for the Mission / Performance workstation. The original M10.9.7.5 candidate is stacked on **M10.9.7.4 Hotfix 1 VALIDATED** and its build/ordinary suite passed, but its focused Windows batch wrapper failed while resolving `run_app_class`; it is therefore superseded/not validated. Hotfix 1 removes batch-label subroutines, invokes every focused test group directly, adds wrapper regression coverage and intentionally introduces no new production behavior.

## Frozen ownership

The closure does not move authority:

- challenge lifecycle, condition semantics and challenge definitions remain owned by M10.9.6;
- score arithmetic and score evidence ownership remain M10.9.6;
- protection/trip state remains canonical plant/protection evidence;
- external demand remains observational training evidence and never writes requested generator load;
- MISSION is a read-only presentation workspace and drill-down is navigation only;
- replay/checkpoint reconstruction remains derived from canonical verified recording evidence;
- session archive schema remains v1;
- `sha256-control-room-snapshot-v1` remains frozen to the populated H29-derived golden anchor.

## Closure matrix

The focused gate exercises the following matrix through the already validated pack/projector/replay/UI contracts plus M10.9.7.5 closure-specific projection checks:

| Closure condition | Evidence owner | Closure expectation |
| --- | --- | --- |
| No active mission | M10.9.7.3 App/UI contract | Explicit `NO ACTIVE MISSION / UNBOUND`; no fabricated mission |
| Active mission without external demand | Initial challenge packs + MISSION projector | Requested and actual output remain visible; external demand/error remain unavailable |
| Bounded demand-following | M10.9.6 demand pack + MISSION projector | Grid demand, requested load and actual output remain distinct |
| Completed mission | MISSION projector | Completed lifecycle and frozen terminal step remain visible |
| Failed mission | MISSION projector | Failed lifecycle and frozen terminal step remain visible |
| Generator trip required evidence | Generator-trip/load-rejection pack | Trip is required evidence, not a global failure |
| Unexpected trip failure | Normal-operation packs | Authored unexpected-trip failure remains explicit |
| Terminal mission while plant time continues | lifecycle logical-step alignment + MISSION projector | Terminal boundary stays frozen while presentation logical step advances |
| Checkpoint/restored mission | M10.9.6 replay + M10.9.7.4 archive/checkpoint contracts | Rebuilt prefix is equivalent and later rows do not survive seek |
| Assistance-mode changes | challenge assistance + MISSION projector | Presentation mode changes without plant-state mutation or score-owner change |
| Requested/effective authority changes | control-authority presentation + MISSION projector | Requested and effective authority remain distinct and observational |

## Cross-cutting closure invariants

Promotion requires all of these to remain true:

- F1–F8 preserved; no F9;
- MISSION plant-command authority is false;
- demand/request/actual are semantically separated;
- score decomposition is copied from the M10.9.6 scoring owner;
- lifecycle/timeline order is deterministic logical-step/canonical-sequence evidence;
- full replay, checkpoint prefix and live continuation preserve equivalent workstation presentation;
- archive exact-pack binding remains explicit and mismatch fails closed;
- unbound archive restoration remains unbound;
- safety/protection presentation remains more prominent than game-like score presentation;
- no wall-clock ordering/scoring authority is introduced.

## Automated gate

Run after build and the complete ordinary suite:

```bat
scripts\run-m1097-mission-performance-closure-audit.cmd
```

The focused gate reruns the relevant M10.9.6 replay closure, M10.9.7.1 projection, M10.9.7.3 live/unbound UI, M10.9.7.4 timeline/archive/drill-down contracts and the M10.9.7.5 closure-matrix tests.

## Manual gate

Complete `M10_9_7_5_MANUAL_VALIDATION_CHECKLIST.md`. This is a closure review, not a request to re-author M10.9.7.4 behavior. It concentrates on minimum-window/keyboard usability, at-a-glance objective-demand-score comprehension, safety hierarchy and useful drill-down return paths.

M10.9.7 becomes VALIDATED only after build, ordinary suite, this focused closure gate and explicit manual closure acceptance are all green. Only then may M10.9.8 begin.
