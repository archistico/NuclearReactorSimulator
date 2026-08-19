# M10.9.4.1-H.4 — Deterministic Hybrid Semi-Implicit Activation & Cost Gate

**Status:** VALIDATED — audit/decision gate complete. The validated source kept production current-v2 explicit at 10 ms and authorized a separate H.5 integration candidate.

## 1. Validated prerequisite

H.3 Hotfix 1 is user-validated. Its isolated semi-implicit prototype converged on all 50 audited intervals and materially reduced primary hydraulic chatter while preserving deterministic repeat, conservation and energy ownership. The supplied evidence reported prototype/explicit chatter ratios of 0.432808 for the main pump, 0.135868 for the channel path and 0.031681 for the return path. The pressure ratio improved more modestly to 0.922127.

The same evidence also showed that evaluating the H.3 corrector on every logical step costs approximately 15.895 times the isolated explicit replay. That cost is not acceptable as a production default.

H.4 therefore does **not** directly activate H.3. It tests whether a deterministic hybrid predictor/corrector can retain enough numerical benefit at bounded work.

## 2. Hybrid numerical contract

For each audit interval H.4 performs the existing one-pass explicit predictor first. The predictor is inspected only through deterministic simulation quantities:

- maximum fractional subcooled-liquid pressure change;
- maximum absolute change of any pipe, valve or pump flow between committed and predicted states.

If neither trigger is crossed, the explicit predictor is accepted. If either trigger is crossed, the H.3 semi-implicit Picard corrector is evaluated from the original committed state with the same frozen non-hydraulic forcing.

No wall-clock value participates in trigger logic, selection logic or simulation state.

## 3. Candidate sweep

H.4 deliberately avoids accepting a single trigger/tuning pair by inspection. The explicit audit sweeps eight deterministic configurations covering:

- pressure-dominant selective correction;
- flow-dominant selective correction;
- combined pressure/flow triggers;
- Picard relaxation 0.10 and 0.15;
- bounded 72- or 96-iteration corrector budgets.

The H.3 0.10 / 96 controls remain historical prototype evidence and are not automatically promoted.

## 4. Deterministic bounded-work metric

Measured wall time remains useful observational evidence but cannot safely decide a deterministic simulation branch. H.4 therefore adds a deterministic work proxy:

```text
work ratio = (logical steps + sum(corrector iteration counts)) / logical steps
```

The activation criterion requires a deterministic work ratio no greater than 4.0. Wall-clock cost is printed beside it but never participates in candidate selection.

## 5. Activation criteria

A configuration is numerically admissible only if all corrected steps converge and the following bounds remain green:

```text
pump chatter ratio       <= 0.80
channel chatter ratio    <= 0.60
return chatter ratio     <= 0.50
pressure ratio           <= 1.00
deterministic work ratio <= 4.00
final mass gap           <= 0.001
final energy gap         <= 0.001
final pressure gap       <= 0.010
```

Existing H.3 conservation/ownership limits remain unchanged:

```text
inventory mass residual       <= 1e-6 kg
inventory energy residual     <= 1e-2 J
hydraulic mass-rate closure   <= 1e-8 kg/s
hydraulic energy residual     <= 1e-3 W
```

The audit reports `activation-criteria-met=True/False`.

## 6. Selection rule

Selection is deterministic:

1. configurations satisfying all activation criteria rank before non-passing configurations;
2. lower deterministic work ratio ranks first;
3. lower combined chatter/pressure score breaks ties;
4. configuration id is the final ordinal tie-break.

Wall time cannot alter the selected configuration.

## 7. Production invariants

H.4 must preserve:

```text
production logical timestep          10 ms
production pressure/flow method      explicit committed-state
hybrid production active             False
adaptive timestep active             False
physical coefficient retuning        False
hidden flow filtering                False
wall-clock adaptation                False
legacy/current-v1 behavior           unchanged
```

Even `activation-criteria-met=True` is only permission to prepare a separate production-integration candidate. H.4 itself never wires `HybridSemiImplicitHydraulicGateSolver` into `PlantNetworkOrchestrator`.

If no configuration passes, production remains explicit and the next numerical step must optimize/redesign the hybrid method rather than weaken the physics or conservation gates.

## 8. Evidence artifacts

The explicit H.4 audit writes UTF-8 **without BOM**:

```text
artifacts/h4-hybrid-semi-implicit-gate/
    01-current-v2-hybrid-sweep.csv
    01-current-v2-hybrid-sweep.summary.txt
    02-current-v2-selected-hybrid-trajectory.csv
    03-current-v2-selected-final-state.csv
```

The H.3 audit writer is also normalized to UTF-8 without BOM so Windows console output no longer prints a leading BOM marker.

## 9. Validated result

User validation passed compilation, ordinary tests and the focused H.4 gate. The deterministic selection was:

```text
selected=P060-F040-R015
pressure trigger=0.060000
flow trigger=40.000 kg/s
relaxation=0.150
maximum corrector iterations=72
corrections=2/50
converged corrections=2/2
deterministic work ratio=2.140000
observational wall-cost ratio=1.662880
activation-criteria-met=True
production-hybrid-active=False
```

Validated chatter ratios hybrid/explicit were pump 0.432616, channel 0.135885, return 0.086691 and pressure 0.921832. Final relative mass/energy/pressure gaps were 0.000000972 / 0.000000943 / 0.000073433. Conservation/ownership residuals remained zero and deterministic repeat was exact.

The result authorizes H.5; it does not retroactively make H.4 a production-hybrid source.
