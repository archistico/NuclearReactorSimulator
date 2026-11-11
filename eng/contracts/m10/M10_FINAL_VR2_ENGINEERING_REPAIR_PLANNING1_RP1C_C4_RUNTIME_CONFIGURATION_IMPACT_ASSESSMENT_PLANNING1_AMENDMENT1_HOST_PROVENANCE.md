# M10 Final - VR2 Engineering Repair Planning 1 - RP1C C4 Runtime Configuration Impact Assessment Planning 1 Amendment 1 - Execution Host Provenance

## Purpose
This is a planning amendment only. It preserves the returned `PASS-AS-AUTHORED` Runtime Configuration Impact Assessment Planning 1 design and adds the execution-host controls required after disclosure that the project is tested on multiple PCs with materially different performance.

The future gate remains `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1`. Profiles, test families, process counts, exact-v9 corpus, C4 candidate, strict threshold and evidence-file count are unchanged.

## Why the amendment is required
A2 compares wall-clock behavior under `AMBIENT-UNSET` and `EXPLICIT-REFERENCE-ALL-ON` and retains the absolute `409.30666666666673 us` strict maximum. Mixing evidence from different physical hosts would confound runtime-profile effects with hardware effects.

The correct unit of comparison is therefore one complete A2 execution on one physical host.

## Frozen same-host rule
A future A2 run must:
- capture one stable execution-host fingerprint before any child run;
- execute both runtime profiles on that same host;
- execute exact-v9, ordinary Release, replay/determinism and non-VR2 performance families on that same host;
- capture host provenance again at the end;
- fail closed if the stable fingerprint changes;
- never merge partial A2 evidence produced on a different host.

Moving the repository between the work PC and home PC between complete gates remains allowed. Moving or combining evidence within one A2 gate is not allowed.

## Host fingerprint and privacy
The host fingerprint is SHA-256 over stable platform fields including the system UUID, CPU name, system manufacturer/model, logical processor count, physical memory and OS version/build.

The raw system UUID, machine name and user name must not be written to evidence. The UUID may contribute only to the SHA-256 fingerprint.

Human-readable evidence records non-secret execution context: CPU name, system manufacturer/model, logical processor count, physical memory, OS version/build, process architecture, .NET framework description, .NET SDK version, Stopwatch frequency, active power scheme and hypervisor state.

## Power configuration
The active Windows power scheme must be recorded at gate start and gate end and must remain unchanged during the gate. A change is infrastructure RED because it invalidates a controlled performance comparison; it is not engineering evidence against C4.

## Performance interpretation
The strict maximum remains exactly `409.30666666666673 us`. The amendment does not relax or scale it.

However, absolute wall-clock observations are host-scoped evidence. Results from the work PC and home PC must not be directly compared as if hardware were controlled. Cross-host extrapolation of the absolute threshold is not authorized.

The primary A2 inference remains the same-host profile contrast:
`AMBIENT-UNSET` versus `EXPLICIT-REFERENCE-ALL-ON`.

A slower host cannot be used to justify changing the threshold, C4 or exact-v9. If cross-host replication becomes materially necessary, it requires a separately planned replication gate after A2 adjudication.

## Evidence tree
The total future A2 evidence count remains 78. Host provenance consumes two of the already planned eight aggregate slots:
1. `01-contract-and-provenance.txt`
2. `02-execution-host-provenance.txt`
3. `03-host-consistency-audit.txt`
4. `04-exact-v9-profile-summary.csv`
5. `05-ordinary-suite-impact-summary.csv`
6. `06-replay-determinism-impact-summary.csv`
7. `07-non-vr2-performance-impact-summary.csv`
8. `08-runtime-configuration-impact-assessment1-summary.txt`

Each child-run contract must carry the root host fingerprint. Exact-v9 runtime-context files must also carry it.

## Retroactive evidence
The available Runtime Factor Isolation 1 and Dynamic PGO Comparator 1 runtime contexts agree on .NET, OS, architecture, logical processor count and Stopwatch frequency. That supports consistency but is not a retroactive proof of physical-host identity.

Historical evidence remains frozen as returned.

## Authority boundary
Planning 1 remains `PASS-AS-AUTHORED`, but Planning 1 alone is no longer sufficient to implement A2. A2 implementation may become eligible only after this amendment returns and is adjudicated PASS.

RP1C selection, production runtime change/repair, FDPC2, threshold/exact-v9 change, VR3, P3-R1 and second replacement-long remain unauthorized.
