# M10 Final - VR2 Engineering Repair Planning 1 - RP1C C4 Exact-v9 Dynamic PGO Comparator 1

## Status

**CANDIDATE - evidence-only implementation authorized by returned Planning 1 adjudication.**

This gate answers one bounded question: with TieredCompilation, QuickJit, QuickJitForLoops and ReadyToRun fixed ON, does changing only `DOTNET_TieredPGO` materially alter the frozen exact-v9 timing distribution or rare strict tail?

The gate does not choose a production runtime configuration and does not auto-promote a causal claim.

## Frozen runtime modes

| Mode | TieredCompilation | TieredPGO | QuickJit | QuickJitForLoops | ReadyToRun |
| --- | ---: | ---: | ---: | ---: | ---: |
| `PGO-OFF-QJFL-ON` | 1 | 0 | 1 | 1 | 1 |
| `PGO-ON-QJFL-ON` | 1 | 1 | 1 | 1 | 1 |

Only `DOTNET_TieredPGO` changes.

## Measurement contract

- C4 remains immutable.
- The RP1A exact-v9 corpus remains immutable at 360 rows.
- 16 warm-up passes and 64 measured passes are retained.
- Rotation stride remains 37.
- Each process records 23,040 measured calls.
- Five fresh processes are executed per mode, ten total.
- Total measured calls are 230,400.
- Strict max remains `409.30666666666673 us` from the frozen RP1A performance baseline.
- `100 us` remains a diagnostic tail floor only and is not an acceptance threshold.
- The measured harness must remain allocation-neutral.
- Candidate allocation, harness allocation and Gen0/1/2 collection deltas are recorded.
- The adjudicator verifies all 64 x 360 call identities, order, probe IDs, logical steps and node IDs for every process.
- Negative, neutral or inconclusive engineering evidence is not infrastructure RED.

## Counterbalanced execution order

The frozen ten-process schedule is:

1. OFF1
2. ON1
3. ON2
4. OFF2
5. OFF3
6. ON3
7. ON4
8. OFF4
9. OFF5
10. ON5

No early stopping or post-result sampling extension is permitted.

## Evidence tree

The completed gate must contain exactly 46 files:

- 10 process directories x 4 files = 40;
- 6 aggregate files.

Per process:

1. `01-process-contract.txt`
2. `02-exact-v9-call-timing.csv`
3. `03-runtime-context.txt`
4. `04-process-summary.txt`

Aggregate:

1. `01-contract-and-provenance.txt`
2. `02-pgo-run-summary.csv`
3. `03-pgo-contrast-summary.csv`
4. `04-tail-row-path-summary.csv`
5. `05-pgo-evidence-summary.txt`
6. `06-dynamic-pgo-comparator1-summary.txt`

The run summary includes execution sequence index. The tail summary retains run and run/pass provenance for each diagnostic-tail row/path group.

## Caller environment fail-closed rule

The runner refuses to start if the caller already defines any of:

- `DOTNET_TieredCompilation`
- `DOTNET_TieredPGO`
- `DOTNET_TC_QuickJit`
- `DOTNET_TC_QuickJitForLoops`
- `DOTNET_ReadyToRun`

The runner never silently clears inherited runtime compilation settings.

## Interpretation boundary

The aggregate adjudicator verifies evidence integrity and computes the OFF/ON contrast, but records `automatic-causal-promotion=False`.

Returned-evidence adjudication must separately determine whether any median/p95/tail difference is material, repeated across fresh processes, dependent on isolated maxima, or associated with repeated row/path ownership. Only after that returned adjudication may Branch A2 Runtime Configuration Impact Assessment be planned.

Ambient/effective-default equivalence is deliberately excluded from this gate.

## Authority boundary

The following remain false:

- Runtime Configuration Impact Assessment authorized;
- RP1C selection authorized;
- production runtime change authorized;
- production repair authorized;
- C4 mutation authorized;
- threshold change authorized;
- exact-v9 change authorized;
- VR3 authorized;
- P3-R1 authorized;
- second replacement-long authorized.
