# M10.9.4.1-F.2 Validation Checklist

Status: **VALIDATED — user confirmed compilation, focused gate, ordinary suite and audit on 2026-07-26**

## Build and focused gate

```text
dotnet build
scripts/run-main-steam-relief-tests.cmd
```

Expected focused evidence:

- domain definition and main-steam topology tests pass;
- relief solver and exact-once integration tests pass;
- current-v2 desktop and synchronization factory regressions pass;
- legacy cold-shutdown profile exposes no relief path;
- the explicit pressure sweep writes both required artifacts;
- printed summary reports monotonic capacity, conservative exchange and no turbine bypass.

## Ordinary suite

```text
dotnet test
```

Expected discovery if no unrelated tests change:

```text
passed:   981
failed:   0
skipped:  28 explicit
total:    1009
```

## Cumulative gates

```text
scripts/run-choked-steam-flow-tests.cmd
scripts/run-electrical-protection-implementation-tests.cmd
scripts/run-electrical-protection-trajectory-audit.cmd
scripts/run-generator-grid-bidirectional-tests.cmd
scripts/run-turbine-admission-authority-audit.cmd
scripts/run-turbine-governor-actuator-tracking-audit.cmd
scripts/run-gameplay-long-tests.cmd
scripts/run-operational-envelope-audit.cmd
scripts/run-reference-plant-scale-audit.cmd
```

## Required review

Confirm from the F.2 summary and CSV that:

- normal current-v2 initial pressure remains below 6.5 MPa and the relief is closed;
- the first active sampled point is immediately above set pressure;
- lift reaches 1.0 at 6.7 MPa;
- active atmospheric flow is choked;
- full-lift capacity exceeds 12 kg/s;
- source-node and external mass/energy terms are equal and signed negative;
- no turbine bypass, condenser receiver or manual relief authority has appeared.

## Promotion rule

F.2 was promoted after the user confirmed compilation and all requested tests passed. The supplied audit confirmed a 13.531762568 kg/s full-lift capacity at 6.80 MPa, 33.595745149 MW internal-energy export, monotonic flow and conservative external exchange.
