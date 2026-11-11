# M10 Final - VR2 Engineering Repair Planning 1 - RP1C C4 Runtime Configuration Impact Assessment 1 - Returned-Evidence Adjudication

## Decision

The complete returned 78-file A2 tree is accepted as **valid same-host engineering evidence**.

Frozen classification:

```text
A2-RETURNED-EVIDENCE-ADJUDICATION=PASS
A2-CLASSIFICATION=AMBIENT-UNSET-PROJECT-QUALIFICATION-SUFFICIENT-ON-A2-HOST
FDPC2-PLANNING-AUTHORIZED=True
FDPC2-IMPLEMENTATION-AUTHORIZED=False
```

This adjudication does **not** prove that the internal .NET effective defaults are bit-for-bit identical to the five explicit all-ON settings. It establishes only that, on the frozen A2 host and within the A2 project-impact scope, `AMBIENT-UNSET` is operationally sufficient and no explicit runtime override is materially required for the next qualification step.

## Evidence integrity and host scope

The returned tree contains exactly 78 files and passes the authored evidence-integrity adjudication:

- 10 exact-v9 fresh processes / 230,400 measured calls;
- ordinary Release suite under both profiles;
- M10 replay/determinism under both profiles;
- three M10.9.7.2 non-VR2 hot-path processes per profile;
- one start/end host provenance record;
- seven aggregate/adjudication outputs.

The complete gate ran on one host with stable provenance:

```text
host-fingerprint-sha256=931EAC000C09399B8EAABEB0316F16980620CAC45D1FC7F13516CE55DAF69336
cpu=13th Gen Intel(R) Core(TM) i5-13500
computer=HP Pro SFF 400 G9 Desktop PC
logical-processors=20
memory=8243453952 bytes
os=Windows 10.0.22631 build 22631
process-architecture=X64
dotnet-sdk=10.0.203
stopwatch-frequency=10000000
hypervisor-present=True
power-scheme=8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c (High performance)
```

Start/end host fingerprint and active power scheme match. No cross-host evidence mixing is detected.

## Exact-v9 comparison

Aggregate exact-v9 evidence is:

| Profile | Median of process medians | Median of process p95 | Max | >100 us | >409.306666... us | Reproducible strict owner groups |
|---|---:|---:|---:|---:|---:|---:|
| `AMBIENT-UNSET` | 2.1 us | 2.5 us | 280.6 us | 10 | 0 | 0 |
| `EXPLICIT-REFERENCE-ALL-ON` | 2.0 us | 2.3 us | 416.2 us | 5 | 1 | 0 |

The central distributions are practically aligned for the purpose of project qualification. The explicit reference is slightly lower in median/p95, but the difference is only about 0.1-0.2 us at these aggregate levels and does not create a project-level advantage that justifies an explicit runtime policy.

The sole strict explicit-reference miss is isolated:

```text
process 5
measured pass 10
row 307
probe exact-v9-5-to-6mwe
logical step 302784
node outlet
resolution path C2-MIXTURE-PREFIX
elapsed 416.2 us
allocated 0 B
```

It is not reproduced in a second process and therefore does not establish a strict owner. `AMBIENT-UNSET` records zero strict exceedances in all five processes.

Both profiles have zero unresolved calls, zero candidate/harness allocation in the exact-v9 measured region and zero measured-region GC collections.

## Ordinary Release suite

Both profiles produce identical functional evidence:

```text
reported-total=1513
executed=1371
passed=1371
failed=0
skipped=0
errors=0
not-run=142
all-exit-codes-zero=True
engineering-pass=True
```

No ordinary Release regression is attributable to either profile.

## Replay / determinism

Both profiles return:

```text
executed=17
passed=17
failed=0
skipped=0
errors=0
not-run=0
engineering-pass=True
```

No replay/determinism regression is observed.

## Non-VR2 hot-path

All six M10.9.7.2 hot-path runs pass. Median ratios remain closely aligned:

| Metric | Ambient median ratio | Explicit median ratio |
|---|---:|---:|
| plant definition lookup | 0.04817 | 0.04609 |
| plant state lookup | 0.00918 | 0.00903 |
| observation change tracking | 0.01512 | 0.01472 |
| critical ratio | 0.29678 | 0.29377 |

No unrelated validated performance owner is regressed by `AMBIENT-UNSET`.

## Engineering interpretation

The evidence supports all of the following:

1. both profiles are project-impact neutral over the A2 functional/replay/non-VR2 scope;
2. explicit all-ON settings are not required to obtain the low exact-v9 central distribution on this host;
3. `AMBIENT-UNSET` is the conservative profile for the next qualification step because it requires no production runtime override and recorded zero strict exact-v9 misses in A2;
4. the single explicit-reference strict miss is isolated and does not establish a reproducible runtime or C4 owner;
5. absolute wall-clock conclusions remain scoped to the A2 host; this adjudication is not a cross-machine performance guarantee.

The evidence does **not** justify saying that unset environment variables prove the same internal JIT configuration as explicit all-ON. It also does not authorize a production runtime change.

## Forward authority

Only planning for Full-Domain Performance Confirmation 2 opens:

```text
FDPC2-PLANNING-AUTHORIZED=True
FDPC2-IMPLEMENTATION-AUTHORIZED=False
RP1C-SELECTION-AUTHORIZED=False
PRODUCTION-RUNTIME-CHANGE-AUTHORIZED=False
PRODUCTION-REPAIR-AUTHORIZED=False
THRESHOLD-CHANGE-AUTHORIZED=False
EXACT-V9-CHANGE-AUTHORIZED=False
VR3-AUTHORIZED=False
P3-R1-AUTHORIZED=False
SECOND-REPLACEMENT-LONG-AUTHORIZED=False
```

The planned FDPC2 profile is `AMBIENT-UNSET` on the same physical host and stable power scheme as A2. A different physical host requires a separately adjudicated host-scope amendment rather than silent evidence mixing.
