# M10.9.4.1-H.11 Validation Checklist

**Candidate:** Thermodynamic Switching Localization & Active-Set Diagnosis
**Validated prerequisite:** M10.9.4.1-H.10 Hotfix 1

## Build and ordinary suite

```bat
dotnet build
dotnet test
```

Both must pass with warnings-as-errors and the ordinary xUnit analyzer contract intact.

## Focused H.11 gate

```bat
scripts\run-thermodynamic-switching-localization-audit.cmd
```

Expected artifacts:

```text
artifacts\h11-thermodynamic-switching-localization\
    01-current-v2-thermodynamic-switching-localization.summary.txt
    02-persistent-event-localization.csv
    03-localized-thermodynamic-nodes.csv
    04-thermodynamic-boundary-probes.csv
```

## Required frozen evidence

The audit must reproduce the 500-step explicit trajectory, seven P060/F040 triggers, H.4 5/7, H.6 6/7, H.7 5/7, H.8 5/7 and H.9 5/7 with exactly two persistent H.9 failures.

It must also reproduce the validated H.10 thermodynamic diagnosis:

```text
H10 thermodynamic switch nodes = 2
explicit-end switch nodes = 0
```

## Required H.11 evidence

- only H.10 switching nodes are localized;
- energy-minus/plus and mass-minus/plus probes are reported;
- resolved/out-of-range state, phase, pressure, temperature and vapor quality are recorded;
- saturation-reference distances are recorded when available;
- crossing axis and boundary class are explicit;
- suggested active set is evidence only;
- local mapped-minus-applied hydraulic mass/energy balance residuals are reported;
- exact deterministic repeat is true;
- production remains explicit at 10 ms;
- no H.11 state is committed;
- no solver H.3-H.10 is replaced;
- no trigger/physics retuning, hidden filtering or thermodynamic clamping is introduced.

`thermodynamic-switching-localization-passes=False` would be a valid diagnostic result if the frozen evidence is reproduced deterministically; it would block an active-set formulation and redirect the investigation to fixed-point existence/basin structure.
