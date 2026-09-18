# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Runtime Configuration Impact Assessment Planning 1

## Purpose
This is a **planning-only** Branch A2 gate. It consumes the returned/adjudicated Dynamic PGO Comparator 1 evidence and freezes the future project-wide runtime impact assessment. It does not implement that assessment and does not select a production runtime configuration.

## Adjudicated prerequisite
Dynamic PGO Comparator 1 returned a complete 46-file / 10-process / 230,400-call evidence tree. PGO ON materially and repeatedly reduces central exact-v9 timing with QJFL ON. Rare strict-tail causality is not promoted.

## Future gate
Future gate identity: `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1`.

The future gate is evidence-only. Engineering-negative or inconclusive outcomes must not be converted into infrastructure RED.

### Runtime profiles

| Profile | TieredCompilation | TieredPGO | QuickJit | QuickJitForLoops | ReadyToRun |
| --- | --- | --- | --- | --- | --- |
| `AMBIENT-UNSET` | unset | unset | unset | unset | unset |
| `EXPLICIT-REFERENCE-ALL-ON` | 1 | 1 | 1 | 1 | 1 |

The caller must enter with all five variables unset. The runner must fail closed rather than silently sanitize a contaminated parent environment.

`EXPLICIT-REFERENCE-ALL-ON` is a qualification reference only. The gate must not label it a production recommendation.

## Frozen assessment families

### A. Fresh exact-v9 profile comparison
- 5 fresh processes per profile;
- 10 processes total;
- 360 frozen exact-v9 rows;
- 16 warm-up passes;
- 64 measured passes;
- rotation stride 37;
- 23,040 measured calls per process;
- 230,400 calls total;
- unchanged strict maximum `409.30666666666673 us`;
- diagnostic-only `100 us` floor;
- complete 64 x 360 identity/order check per process;
- raw per-call timing, runtime context and per-process summary retained.

The comparison is used to establish **project-level operational default equivalence**, not to claim internal JIT-switch introspection. Any repeated strict owner uses the already frozen rule: the same row/boundary must exceed the strict ceiling in at least two independent processes before it can be called reproducible.

### B. Ordinary Release suite
Run the ordinary non-explicit Release suite once in a fresh child process under each profile. Both runs preserve the existing test predicates and test counts. A test failure is engineering impact evidence if the infrastructure itself remains valid.

### C. M10 replay/determinism focused set
Run the following existing classes under both profiles:
- `NuclearReactorSimulator.Simulation.Tests.Runtime.SimulationReplayTests`;
- `NuclearReactorSimulator.Simulation.Tests.Runtime.SimulationLongRunDeterminismTests`;
- `NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Replay.M10965ChallengeReplayCheckpointClosureTests`;
- `NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10984ReplayCheckpointSameSeedIntegrityTests`.

No fingerprint or equality predicate may be weakened.

### D. Representative non-VR2 performance owner
Run existing explicit class `NuclearReactorSimulator.Application.Tests.Milestones.M10972Hotfix2TenMillisecondHotPathHardeningTests` in 3 fresh processes per profile. Preserve its existing allocation and relative-performance assertions. Copy its summary/metrics immediately after each process so later runs cannot overwrite evidence.

This sentinel is intentionally outside the exact-v9/C4 micro-path.

## Future evidence tree
The future gate must emit exactly 78 files:
- 40 exact-v9 process files: 10 processes x 4 files;
- 6 ordinary-suite files: 2 profiles x 3 files;
- 6 replay/determinism files: 2 profiles x 3 files;
- 18 non-VR2 performance files: 6 processes x 3 files;
- 8 aggregate files.

## Adjudication questions
Returned A2 evidence must answer, without automatic promotion:
1. Are ambient/unset and explicit-reference profiles operationally equivalent for project qualification?
2. Does the explicit profile preserve the ordinary Release suite?
3. Does it preserve replay/determinism invariants?
4. Does it preserve the unrelated validated hot-path owner?
5. Does either profile reproduce a strict exact-v9 owner in at least two independent processes?
6. Is any explicit production runtime setting actually necessary, or are ambient defaults already sufficient?

Only returned-evidence adjudication may decide whether A2 is green and whether Full-Domain Performance Confirmation 2 planning can open.

## Authority boundary
All of the following remain false: RP1C selection, production runtime change, production repair, C4 mutation, threshold change, exact-v9 change, Full-Domain Performance Confirmation 2 authorization, VR3, P3-R1 and second replacement-long authorization.
