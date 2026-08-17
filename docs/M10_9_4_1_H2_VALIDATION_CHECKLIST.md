# M10.9.4.1-H.2 Validation Checklist

H.2 is a method-selection/documentation checkpoint. It must not change Simulation physics or activate a semi-implicit runtime path.

## Gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-numerical-stiffness-decision-audit.cmd
```

Expected ordinary inventory remains unchanged from validated H.1:

```text
passed:   1031
failed:      0
skipped:    34 explicit
total:    1065
```

The H.1 audit must continue to report the validated evidence pattern, including `refinement-improves=False`, with production fixed step at 10 ms and no semi-implicit treatment active.

## Source-delta review

Confirm:

- no file under `src/NuclearReactorSimulator.Simulation` changes in H.2;
- no physical coefficient, resistance, pump boost, controller tuning, protection threshold or turbine parameter changes;
- Application descriptor identifies H.2 as a decision candidate and states that no semi-implicit runtime path is active;
- roadmap/handoff record H.3 prototype and H.4 activation gates before Phase I;
- ADR 0126 records why explicit fixed-step retention and bounded explicit substeps were rejected as the preferred cure.

Do not mark H.2 validated merely because documentation builds. Promote it only after the ordinary suite remains green and the H.1 evidence gate is reproducible from the H.2 source.
