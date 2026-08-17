# M10.9.4.1-G.2 validation checklist

> **Completed:** G.2 Hotfix 2 was user-validated on 2026-07-26. The expected values below are the promotion evidence for that checkpoint.

## Build and focused gate

```bat
APPLY_UPDATE.cmd
dotnet build
scripts\run-passive-hydraulic-enthalpy-tests.cmd
```

Expected focused behavior:

- `PipeDefinition` defaults to historical internal-energy transport;
- explicit enthalpy mode is accepted and unknown modes fail closed;
- pipe and valve endpoint balances use `h*m_dot` when selected;
- reverse flow uses the actual upstream node;
- current-v2 passive pipes/valves opt in while legacy profiles and pump paths remain historical;
- pump hydraulic work and shaft demand are each counted exactly once;
- all three G.2 artifacts are generated and the summary is printed.

## Hotfix 1 stability-envelope regression

The ten-second desktop continuation must remain within **2940–3050 rpm**, produce more than 4.5 MW shaft power, and retain both `TripCommandActive=False` and `OverspeedDetected=False`. The 2940 rpm lower bound corresponds to 49.0 Hz and remains above the 48.8 Hz underfrequency pickup. Do not retune runtime physics to recover the superseded 2950 rpm edge.

## Ordinary suite

```bat
dotnet test
```

Expected ordinary discovery after G.2 additions:

```text
passed:   1015
failed:   0
skipped:  31 explicit
total:    1046
```

## Cumulative gates

```bat
scripts\run-open-control-volume-energy-tests.cmd
scripts\run-turbine-bypass-tests.cmd
scripts\run-main-steam-relief-tests.cmd
scripts\run-choked-steam-flow-tests.cmd
scripts\run-electrical-protection-implementation-tests.cmd
scripts\run-electrical-protection-trajectory-audit.cmd
scripts\run-generator-grid-bidirectional-tests.cmd
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

## Promotion evidence

Promote G.2 only after the user confirms:

- clean build with zero warnings/errors;
- focused G.2 gate passes, including the explicit audit;
- ordinary suite passes;
- cumulative long-running and audit gates pass;
- generated summary shows zero passive transfer closure, zero pump fluid-work residual and zero positive shaft-efficiency residual.
