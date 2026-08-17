# M10.9.4.1-H.12 Validation Checklist

## Baseline

- [ ] Candidate is applied over the user-validated M10.9.4.1-H.11 baseline.
- [ ] Production current-v2 remains `ExplicitCommittedState` at 10 ms.
- [ ] No H.3-H.9 corrector is routed into production.

## Build and ordinary suite

```bat
dotnet build
dotnet test
```

- [ ] Build passes with warnings-as-errors.
- [ ] Ordinary test suite passes.

## Focused H.12 gate

```bat
scripts\run-thermodynamic-inverse-branch-selection-audit.cmd
```

- [ ] Simulation unit tests for inverse branch diagnosis pass.
- [ ] Application descriptor regression passes.
- [ ] Frozen P060/F040 evidence is reproduced: 500 intervals, 7 triggers, H.4 5/7, H.6 6/7, H.7 5/7, H.8 5/7, H.9 5/7 with two persistent failures.
- [ ] H.11 localizes exactly two nodes and H.12 diagnoses exactly two nodes.
- [ ] Production-selected phase matches the H.11 resolved phase for every probe.
- [ ] Every probe reports exactly the five existing branch attempts.
- [ ] Deterministic repeat is true.
- [ ] `thermodynamic-inverse-branch-selection-audit-passes=True`.

## Required artifacts

- [ ] `01-current-v2-thermodynamic-inverse-branch-selection.summary.txt`
- [ ] `02-persistent-event-branch-selection.csv`
- [ ] `03-node-branch-mechanisms.csv`
- [ ] `04-probe-branch-selection.csv`
- [ ] `05-branch-candidates.csv`

## Interpretation

`overlapping-root-coarse-priority-mechanism-confirmed=False` does **not** by itself fail H.12. H.12 is a diagnostic milestone. A negative mechanism result means the detailed candidate evidence must be inspected before choosing the next numerical formulation.

## Forbidden changes

- [ ] Do not reorder production thermodynamic branches.
- [ ] Do not add previous-state hysteresis to production.
- [ ] Do not enforce H.11 suggested active sets.
- [ ] Do not widen/clamp the thermodynamic envelope.
- [ ] Do not retune P060/F040, physical coefficients, residual tolerances or timestep.
- [ ] Do not commit shadow candidates.
