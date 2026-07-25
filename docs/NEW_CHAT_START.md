> **Current continuation candidate:** cumulative M10.9.4.1 D.3.2 Hotfix 3 + E.2 Hotfix 1 + operator turbine-valve station. M10.9.4 is still the validated baseline. Fast gates are green; long-running and manual gates are pending.

# Active continuation — cumulative M10.9.4.1 D/E/operator-authority candidate

D.1, D.2 and D.2 Hotfix 1 are locally user-validated. D.3.1 adds current-v2 passive rotor loss; D.3.2 makes the complete admission train authoritative; Hotfix 3 changes only the loaded desktop main-steam-line resistance from 1,000 to 850 Pa·s²/kg². E.1 accepts a 10 MWe current-v2 target and E.2 plus signed-torque Hotfix 1 implements bidirectional generation/motoring. The operator station exposes canonical stop/admission OPEN/CLOSE, control-valve AUTO/MANUAL and explicit manual demand. On 2026-07-25 build, ordinary suite and all focused D/E audits passed. Run long and manual gates before promotion or E.3.

# Superseded D.3 audit bootstrap — retained as historical evidence

D.1, D.2 and D.2 Hotfix 1 are locally user-validated. Use D.3.1 as the active source. D.3 evidence proved that the breaker-open rotor could remain indefinitely near 3301 rpm with zero valve, zero effective steam flow and zero shaft power because no passive load torque existed. D.3.1 adds optional 0.5 MW rated-speed loss only to sustained current-v2 profiles. Do not add tracking anti-windup until the corrected post-loss evidence is reviewed; do not compensate for the current +0.75 rpm per 5 MWe droop displacement before Phase E scale work.

# New Chat Start — Nuclear Reactor Simulator

We are continuing the **Nuclear Reactor Simulator** project.

## Read first

1. `docs/PROJECT_HANDOFF.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ROADMAP.md`
4. `docs/ARCHITECTURE.md`
5. `docs/milestones/M10.9.4.md`
6. `docs/SUBSYSTEM_ENGINEERING_SCHEMATICS.md`
7. `docs/M10_9_4_1_A_EXTENDED_AUDIT.md`
8. `docs/M10_9_4_1_EXTERNAL_TECHNICAL_AUDIT_REVIEW.md`
9. `docs/OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md`
10. `docs/REFERENCE_PLANT_SCALE_CONTRACT.md`
11. `docs/REFERENCE_PLANT_SCALE_EVIDENCE.md`
12. `docs/KNOWN_MODEL_LIMITATIONS.md`
13. `docs/GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md`
14. `docs/milestones/M10.9.3.md`
15. `docs/INTERACTIVE_FULL_PLANT_MIMIC.md`
16. `docs/ADVANCED_INSTRUMENT_GAUGE_SYSTEM.md`
17. `docs/OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md`
18. `docs/usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md`
19. ADR 0075–ADR 0094

## Exact checkpoint

- M7, M8, M9 gates: **COMPLETE / VALIDATED**.
- M10.1–M10.9.4: **VALIDATED**.
- Official milestone baseline: **M10.9.4 — Subsystem Engineering Schematics**.
- Hotfix 23 and final M10.9.4 manual HMI/schematic checklist: **PASSED**.
- The former ~70 s A-audit trip was resolved in the current-v2 operating seed, not by weakening protection. Root cause: only 20% direct coolant heat deposition, ~0.07 kg/s primary circulation, ~13 kg/s drum steam export.
- Corrected current-v2 seed: conservative fuel/structure→coolant links, matched primary hydraulic resistance, aligned steam-line initial conditions/control-valve bias; v1 seeds and protection thresholds unchanged.
- User validation: exact 300-second sustained journey **PASSED in 2m 07s**; explicit 60-second synchronization journey **PASSED**; build **0 warnings / 0 errors**; ordinary suite **895 passed / 11 explicit skipped / 0 failed**.
- A.3 is historical pre-migration evidence; E.1 accepted 10 MWe and E.2 implements the coordinated current-v2 migration. Isolated one-line scale edits remain prohibited.
- M10.9.4.1-B.1 — Steam-Drum Liquid Inventory Closure: **USER VALIDATED LOCALLY** (compilation and tests passed).
- M10.9.4.1-B.2 — Drum-to-Main-Steam Source Closure: **USER VALIDATED LOCALLY** (compilation and tests passed).
- Current activity: **promote the cumulative D.3.2 Hotfix 3 + E.2 Hotfix 1 + operator-valve candidate**. Fast evidence is green: build 0 warnings/errors; ordinary 944 passed / 17 explicit skipped; admission 3/3; governor 2/2; scale/migration 2/2. Remaining: both 60-second journeys, complete operational-envelope audit and manual PLANT/TURBINE/GENERATOR checks. The historical 300-second wall-clock observation remains open for Phase H.
- B.1 also adds explicit HMI feedback for speed/load references and documents that game penalties are triggered only by accepted manual commands.
- `SPEED RAISE/LOWER`: ±10 rpm per accepted press. `LOAD RAISE/LOWER`: ±5 MWe per accepted press.
- M10 closes only after **M10.9.8 — Integrated Human-Automation-HMI Validation Gate**.

## Approved forward sequence

1. Run both explicit 60-second journeys.
2. Run the complete operational-envelope audit, including the 300-second sustained trajectory and replay/checkpoint cases.
3. Manually validate PLANT rendering, TURBINE valve target/actual/authority behavior and GENERATOR signed -10..+10 MWe presentation.
4. Promote D.3.2/E.1/E.2/operator authority only if all gates are green; otherwise patch the smallest canonical owner and restart the fast gate.
5. Implement E.3 reverse-power, supervised-underfrequency and loss-of-synchronism protection as a separate evidence-first candidate.
6. Continue with Phase F relief/bypass and choked flow.
7. Continue with Phase G flow-work and enthalpy transport.
8. Execute Phase H numerical stiffness decision gate.
9. Close Phase I compatibility and engineering hardening.
10. Advance through M10.9.5–M10.9.8; M10 closes only at the integrated validation gate.

## Why M10.9.4.1 is separate

Hotfix 13–23 repaired real physical/control defects discovered by the M10.9.4 long-running acceptance tests. The new 300-second audit then exposed a further long-horizon trip near 70 simulated seconds. M10.9.4.1 is therefore organized as evidence-first hardening:

- A.1 identifies the exact protection edge, condenser limiter, inventory slopes, pump/controller behavior and nominal scale;
- B–G isolate drum/source, condenser, turbine/governor, generator/grid, relief/choked-flow and enthalpy/flow-work corrections;
- H measures stiffness before choosing explicit substepping or semi-implicit treatment;
- I closes compatibility, CI, reference trajectories, known limitations and possible offline seed trim.

These items are mandatory before command-consequence and challenge/demand work so later features describe stable canonical behavior rather than temporary approximations.

## What M10.9.4 changes

- five detailed Application-owned engineering schematics: reactor/core, primary/steam-drum, turbine/secondary, generator/grid, instrumentation/control/protection;
- explicit IN/OUT and process/signal directions;
- distinct process/energy vs signal-flow grammar;
- generator GRID diagnostic separating shaft power, requested load, actual MWe, synchronization, breaker and protection state;
- requested generator load promoted as presentation-only data;
- explicit explanation that amber SHAFT means mechanical energy, not warning;
- separately runnable xUnit v3 explicit long-gameplay acceptance tests for sustained turbine→generator→grid behavior;
- versioned v2 generation-ready desktop/synchronization seeds while historical v1 origins remain exact;
- HMI turbine steam admission now uses actual effective stage-group flow instead of the legacy zero-valued M4.1 boundary seam.

## Important open verification

The first executable long-gameplay gate **did expose a real integrated balance defect** in the historical v1 operating seeds: the desktop journey reached ~1442.6 rpm / ~2.406 MWe with zero MODEL rotor shaft power after 10 simulated seconds, and the synchronization journey drove `control-out` outside the supported simplified water/steam envelope. Hotfix 6 preserves those historical v1 replay origins and introduces generation-ready v2 seeds with staged steam pressure, matched admission/condenser/feedwater capacity and bumpless control bias. The ordinary suite and explicit 60-second pack must both pass before M10.9.4 can be validated.

## Architecture rules that must not be broken

- deterministic fixed timestep independent of wall-clock/UI cadence;
- canonical M2/M3/M4/M5 ownership unchanged;
- measured consumers never substitute true/model state;
- protection overrides normal/supervisory control;
- Application owns presentation topology; Avalonia renders only;
- no UI-side predictive physics;
- save/load/checkpoint remains M9.1 replay-backed;
- no free-form/NLP control surface.

## Validate M10.9.4

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
scripts\run-gameplay-long-tests.cmd
```

Then manually verify `docs/milestones/M10.9.4.md`.

If the normal suite passes but a long gameplay test fails, diagnose the reported checkpoint/power-path evidence before promoting M10.9.4.


## M10.9.4.1-A observed failure

The extended audit has been run. Compilation and ordinary tests pass, but the intended healthy 300-second/5 MWe journey trips at checkpoint 7/30, step 7000 (~70 s), with turbine and generator trip latched. Conservation remains closed; sampled drift reaches drum 7.821 MPa / 100% level, condenser 28.593 kPa and feedwater flow 0 kg/s.

The combined trip action, bounded rotor speed and bounded frequency identify `condenser-high-backpressure` as the initiating function. A.2 adds one-second direct function/limiter evidence and tests a narrow capacity-headroom correction without changing the 30 kPa threshold or the condenser solver law.

## M10.9.4 Hotfix 13 current correction

Hotfix 13 is a deliberate rebase on Hotfix 10, the last locally reported ordinary-green candidate. Hotfix 11/12 workaround branches are not part of the current package. The root cause is structural: the historical stage-flow law `min(stop, control, admission)` made the admission-train inventory monotonic because all three valves already transfer real mass while the stage source term drains only `turbine-inlet`. Current v2 uses an explicit pressure-driven turbine-inlet→exhaust expansion resistance; legacy null definitions retain the old law only for isolated compatibility.

Repeated `exhaust` failures were traced upstream of the condenser: the v2 recipe initialized both drum `steam` and main-steam `header` at 280 °C saturation, yielding zero canonical `steam → header` pressure difference and therefore zero main-steam-line replenishment. Downstream staged inventories initially masked this by feeding the turbine until they drained. Hotfix 8 adds an optional backward-compatible header steam initialization and uses a continuous v2 pressure staircase 280 / 275 / 269.5 / 253 / 246 °C through turbine inlet, targeting roughly 13 kg/s across every canonical steam-path element. v1 remains unchanged. Standard build/test must pass before the explicit 60-second gameplay pack is rerun.

## M10.9.4 Hotfix 15 historical correction

The Hotfix 14 ordinary suite is locally green. Long gameplay exposed a second monotonic inventory defect in the steam drum: historical separation cancelled canonical return inflow while leaving feedwater as a one-way addition. Hotfix 15 changes current v2 liquid recirculation to committed MCP demand, removing `dm_drum/dt = F_feedwater >= 0` by construction. Legacy behavior is isolated explicitly; current-model correctness has priority.


## M10.9.4.1-C.2 authoritative continuation

Historical continuation note: the supplied consolidated source resumed at Phase D, where D.3 evidence exposed missing breaker-open passive drag. That issue is now included in the cumulative candidate together with D.3.2, the accepted/implemented E scale migration and operator valve authority. Tracking anti-windup remains deferred because focused evidence is green without it. The earlier 300-second wall-clock performance-budget observation remains tracked separately.
