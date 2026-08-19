# M10.9.4.1-I.3 — Reference Trajectories, Conservation/Inventory Baseline & Tolerance Budgets

## Status

**CANDIDATE.** Built directly on user-validated I.2. H.30 remains closed as `OPT-IN ONLY`; exact v2 `ExplicitCommittedState` remains authoritative default/rollback/reference and exact v3 corrected ownership remains qualified opt-in.

I.3 changes no plant physics, numerical mathematics, production selector, persistence semantics or 10 ms fixed step.

## Purpose

I.1 established exact-version compatibility. I.2 established the Phase-I audit/CI execution topology. I.3 now creates the quantitative reference baseline needed by the M10.9.4.1 acceptance gate.

The milestone has three responsibilities:

1. run the exact-v2 healthy desktop reference point for 300 simulated seconds;
2. consolidate conservation and selected fluid-inventory observations into one read-only evidence stream;
3. derive a first versioned tolerance-budget artifact from the validated final-window behavior, to be frozen for future regression rather than used to tune the current runtime.

This extends the existing M9.6 versioned-reference philosophy; it does not create another physics owner.

## Exact trajectory contract

```text
trajectory id:       phase-i-desktop-v2-healthy-300s-v1
initial condition:   integrated-operations-desktop-stable@2
numerical policy:    ExplicitCommittedState
fixed step:          10 ms
logical steps:       30,000
simulated duration:  300 s
sample stride:       100 steps = 1 s
samples:             301 including t=0
final slope window:  final 60 s
```

The static contract is stored in `eng/phase-i-reference-trajectory-contract.csv`.

## Consolidated observations

Every one-second sample records:

- presentation fingerprint;
- trip state and generator breaker state;
- requested/gross electrical power, shaft power and rotor speed;
- condenser pressure and steam-drum liquid level;
- total fluid mass and total fluid internal energy;
- condenser exhaust mass;
- hotwell mass;
- feedwater-inventory mass;
- steam-drum inventory mass;
- main-steam header mass;
- plant mass/energy closure residuals;
- network balance mass-rate/power residuals.

The final 60 seconds additionally produce least-squares slopes for all listed conserved inventories.

## Baseline-establishing tolerance budgets

I.3 intentionally does **not** invent externally calibrated plant tolerances. For ordinary operating variables and inventories, each v1 regression budget is derived from the validated final 60-second window:

```text
target = final-window mean
absolute tolerance = max(explicit engineering floor, 2 × observed maximum deviation)
```

For inventory slopes:

```text
target slope = 0
absolute slope tolerance = max(explicit floor, 2 × |validated observed slope|)
```

The initial I.3 gate is allowed to establish these budgets only after strict independent invariants are green: the 300 operating samples from t=1 s through t=300 s remain trip-free, breaker-closed and generation-healthy, while the full 301-sample reference series including t=0 respects the existing conservation closure limits. Once the user validates I.3, the generated budget file becomes immutable regression evidence for later Phase-I work.

This is a regression envelope, not a claim of external historical measurement and not a license to tune physics to match a stored CSV.

## Hard pass criteria

The 300-second run must keep:

```text
for operating samples t=1..300 s: AnyTripActive = false
for operating samples t=1..300 s: generator breaker = closed
for operating samples t=1..300 s: requested electrical power > 4.5 MWe
for operating samples t=1..300 s: gross electrical output > 4.0 MWe
for operating samples t=1..300 s: shaft power > 4.5 MW
t=0 is retained as the initial reference sample and is not used as a post-Run generation-health point
|max mass closure| <= 1e-6 kg
|max energy closure| <= 1e-2 J
|max balance mass rate| <= 1e-8 kg/s
|max balance power| <= 1e-3 W
all final-window inventory slopes finite
all derived tolerance budgets finite and positive
```

No protection threshold, physics coefficient, seed value or numerical tolerance is changed by I.3.

## Produced artifacts

The focused gate writes:

```text
00-progress.txt
01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt
02-reference-trajectory-contract.csv
03-reference-trajectory-samples.csv
04-conservation-inventory-final-window-slopes.csv
05-versioned-tolerance-budgets.csv
```

## Focused gate

```bat
scripts\run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd
```

Required final flags:

```text
phase-i-reference-trajectory-baseline-passes=True
phase-i-conservation-inventory-baseline-passes=True
i3-audit-passes=True
phase-i-reference-tolerance-baseline-established=True
```

## After a green I.3

Freeze the exact I.3 summary, samples, slopes and budgets as the Phase-I v1 reference baseline. Continue with known-limitations/compatibility closure and the remaining cumulative M10.9.4.1 acceptance evidence. Do not begin M10.9.5 yet and do not retire H.5/H.21 merely because current CI no longer executes them.
