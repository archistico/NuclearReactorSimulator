# R3 Post-Repair Dynamic-Equilibrium Residual Diagnostic 1

The execution status and next activity are recorded in [PROJECT.md](PROJECT.md).

This test-only diagnostic implements the successor authorized by the locally passed
[Replanning 1 audit](M10_FINAL_VR2_R3_POST_REPAIR_DYNAMIC_EQUILIBRIUM_REPLANNING1.md).
Family B production, C4, the raw seed, controller settings, thresholds and canonical
Exact-V9 remain byte-identical. No equilibrium reconstruction is selected or implemented.

## Workload and evidence

Both factories run at 10 ms: canonical mode 1 and the opt-in reference-consistent mode 2.
Each executes exactly 100 Running steps. Observations cover raw inventory, seed-step1,
post-seed (checkpoint 0), and Running steps 1, 2, 5, 10, 20, 50 and 100.
The test-only reconstruction of the two seed steps is checked against each actual
factory for all 12 node inventories/states, circulation, controller, rotor, generator
and control-valve states before using its seed-step1 observations.

| File | Evidence |
| --- | --- |
| `00-static-audit.txt` | Frozen production, previous tests and prerequisite hashes |
| `00-build.txt` | SDK/runtime environment and fresh Release build |
| `00-test.txt` | Explicit single-test execution result |
| `01-node-residuals.csv` | 240 rows: all 12 nodes, states and one-step storage residuals |
| `02-hydraulics-drum.csv` | 18 rows: branch flows, pump speed/boost, heads and drum inventory |
| `03-controller-rotor.csv` | 18 rows: speed control, integral, valve, rotor, torque and power |
| `04-conservation.csv` | 18 rows: suction identity and production network conservation audit |
| `05-health.csv` | 204 rows: both seed snapshots plus all 100 steps for each mode |
| `06-diagnostic-summary.json` | Derived observations; reconstruction family remains unselected |
| `07-manifest.csv` | SHA-256 of the execution logs and diagnostic payloads |

Node storage residuals are `(inventory_after - inventory_before) / 0.01 s`.
They measure departure from an operating-point equilibrium; they are **not** independent
conservation errors. The raw rows have no predecessor and use `null` residuals.
The suction identity independently reconstructs recirculated-liquid inflow minus pump
outflow using the preceding inventory's selected transport energy. Network closure
comes directly from the production thermofluid audit. Hydraulic heads are observed
at the reported snapshot and must not be mistaken for the preceding step's solver inputs.

The historical 100-step envelope is recorded unchanged as diagnostic evidence. Its
known violations do not fail this evidence-completeness test. Non-finite state, trips,
breaker opening, rollback, missing checkpoints or identity/count drift fail the gate.
Passing the diagnostic does not pass R3.

## Local execution

```powershell
.\scripts\run-r3-post-repair-dynamic-equilibrium-residual-diagnostic1.cmd
```

The runner removes only its named diagnostic outputs under `artifacts/r3-dyn-eq-d1`,
validates frozen prerequisites, builds with `--no-incremental --warnaserror`, executes
the explicit opt-in test, and validates every expected mode/checkpoint/node identity.
It produces no successful summary/manifest when any preceding stage fails.
Complete ordinary regression remains a separate gate: `.\eng\ci-ordinary.cmd`.

## Interpretation of the first local evidence

The primary hydraulic operating point is already dynamically displaced during seed
preconditioning. At seed-step1, candidate `outlet` stores energy at approximately
`-1.156615 MW`, while `turbine-inlet` stores approximately `+1.006737 MW`, despite
near-zero initial node mass accumulation. A pressure/phase target roundtrip therefore
does not establish a whole-cycle energy equilibrium. Over the Running second the
pump flow increases from `99.99537721912854` to `100.41741075778903 kg/s`.

The initial rotor torque is nearly balanced (`-0.00012986934302716691 N m` at
seed-step1), but becomes negative during the second seed step and reaches
`-17.981140231276413 N m` at Running step 100. Rotor speed falls to
`2999.8698454382456 rpm`; canonical mode 1 remains at `2999.9999997443929 rpm`.
The candidate's governor increases by `0.16391018764131715` percentage points from
seed-step1; its integral increases by only `0.0010196123699408588`.

These observations support a governor response to a developing rotor power deficit.
They do **not** independently establish a controller-initialization defect. The nearly
identical governor drift before and after Family B means common-mode **across that
repair**, not common-mode between canonical mode 1 and candidate mode 2. An independent
controller owner must not be selected merely from that historical comparison.

All observed health sentinels remain green. Candidate envelope violations remain 84.
Family B's repaired causal seam is preserved. The evidence identifies nonstationarity
in the conserved-inventory/seed-preconditioning composition before any gain or
controller-state adjustment can be justified; it does not prove a unique corrected seed
or distinguish every downstream secondary-cycle energy owner.

The maximum observed suction energy-identity residual is
`6.7815184593200684E-05 W`; maximum production network mass/energy closure residuals
are `7.2759576141834259E-12 kg` and `1.7909030721057216E-05 J`. These are measured
conservation observations, distinct from the nonzero equilibrium storage rates.

## Validation provenance

The planning prerequisite, fresh focused Release build, explicit diagnostic,
complete evidence validator, complete ordinary suite and current-evidence audits
passed locally. The ordinary runner reports 1,535 discovered tests: 1,376 passed,
159 explicit tests not run, zero failures. This diagnostic ran separately with
`--explicit only` and `--minimum-expected-tests 1`.

Negative evidence checks confirmed that a duplicate health identity (unchanged row
count) and a missing node row are rejected; the original evidence bytes were restored.
The returned compact payloads and execution logs are preserved under
`eng/frozen-evidence/r3-dyn-eq-d1`; `07-manifest.csv` authenticates the diagnostic
files, and `08-regression-summary.json` records the ordinary-log SHA-256 without
duplicating its large raw log. The raw ordinary log remains local under `artifacts/`.

## Continuation boundary

Review the complete returned evidence before planning hydraulic-seed (H), controller-state
(C) or joint (J) reconstruction. No family is selected by this diagnostic. Reconstructing
an equilibrium, changing the raw seed/controller state, retuning physics or gains,
relaxing thresholds, changing C4/default mode/Exact-V9, starting Requalification 3 or
R4 remains outside this diagnostic's authority. The next work is evidence review and
equilibrium reconstruction planning only.
