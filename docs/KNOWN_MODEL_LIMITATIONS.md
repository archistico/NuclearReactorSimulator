# Known model limitations

This register contains **current** limitations only. Resolved investigations and milestone chronology belong under `history/`, milestone records and ADRs.

## Thermodynamics and fluid mechanics

- Water/steam properties remain reduced-order educational correlations, not an industrial IAPWS/steam-table implementation across the full operating envelope.
- Authoritative desktop exact `@4` uses `CorrelationConsistentInverseDomain`. Its superheated branch is anchored to the same correlated saturated-vapor boundary used by the saturated branch, and its saturated inverse search is interval-aware around the water-density maximum. The validated repair census resolved the three observed historical no-root states, 7/7 two-sided seam probes and 231/231 low-temperature probes; the repaired 7000-step load journey, 30,000-interval cross-profile run, replay/protection/off-design matrix and performance/soak gates are green. The former vapor seam gap/overlap and 4–8 °C inverse-search blind spot are therefore **resolved for authoritative exact @4 within the validated domain**, not current production blockers.
- Historical exact desktop `@2` and `@3` deliberately retain `HistoricalCorrelationTopology` for exact-version replay compatibility. Their old gap/overlap and low-temperature inverse-search behavior is historical compatibility semantics, not the authoritative production thermodynamic closure. Old saves/scenarios must not be silently reinterpreted through @4.
- `CorrelationConsistentInverseDomain` is still a simplified closure. Matching the phase boundary removes an internal inverse-map inconsistency; it does not make the model a complete high-fidelity steam-table implementation, nor does it validate arbitrary metastable/supercritical states outside the declared envelope. Genuinely unsupported states continue to fail closed.
- Fluid nodes and most components are zero-dimensional lumped control volumes.
- Pipe/valve hydraulics use reduced resistance laws; general distributed pressure loss, elevation/static head, acoustic waves and water hammer are not modeled.

- The passive reduced quadratic hydraulic inversion is continuous through zero flow but has unbounded local slope as driving pressure approaches zero; the ideal pump discharge check transition is continuous but non-smooth. No epsilon/laminar/check-leakage regularization is currently claimed. M12.2 owns the frozen near-zero/conditioning audit before any trajectory-changing law change.
- `ThermodynamicBranchContinuityModel` retains historical “shadow” naming/provenance, but in the authoritative corrected-commit path its bounded previous-phase selection can contribute to a committed candidate when trigger/authority contracts permit. The base inverse resolver remains memoryless; retirement or unification requires separate evidence.
- Pump hydraulic power and positive shaft demand are modeled, while motor/electrical dynamics and inefficiency-to-heat deposition were deliberately outside the original M1.5 primitive. M12.4 must close that ownership before stronger severe-accident/full-plant energy claims.
- Generic `SimulationRuntime.Advance(elapsed)` has no intrinsic per-call catch-up ceiling. The desktop production path is separately bounded through fixed cooperative step batches; M11.3 owns the supported generic-API/catch-up policy audit.
- General wet-steam/two-phase critical-flow and choking fidelity remains limited to explicitly implemented reduced-order paths.
- Cavitation/NPSH, detailed non-condensable gases and full circulating-water dynamics are not modeled.
- Drum swell/shrink and detailed separator carryover/carryunder are not modeled at industrial fidelity.
- Main-steam relief and turbine-bypass opening are stateless pressure functions. They do not model safety-valve blowdown/reseat hysteresis, lift dynamics or actuator memory; reopening and reclosing therefore use the same pressure threshold family. This is a deliberate reduced-order limitation, not a hidden state elsewhere.

## Validated reference drift / inventory redistribution

Historical validated I.3 was continuously healthy over 300 s on the then-authoritative exact-v3 production path, but the final 60 s are **not a claim of asymptotic steady state**. Exact @3 is now frozen provenance; exact @4 is current authoritative production and has separately passed the final repaired-v4 300 s reference requalification against all 19 unchanged I.3 budgets rather than inheriting exact-v3 evidence. The frozen regression observations include:

- drum inventory slope: approximately **+8.245 kg/s**;
- main-steam-header inventory slope: approximately **-0.3529 kg/s**;
- total fluid internal-energy slope: approximately **-2.062 MW**.

Exact regression values are frozen in `../eng/frozen-evidence/ordinary/I3_ValidatedAuthoritativeReferenceSlopes.csv` and `../eng/evidence-manifests/i3-validated.csv`. These values are a regression baseline, not a claim of asymptotic steady state and not calibration targets. They indicate continuing reduced-order inventory/energy redistribution over the reference window. Future work may investigate whether a longer horizon, improved initialization/trim, or a separately validated physical model should reduce those drifts; production physics must not be tuned merely to make the slopes approach zero.

- The first M10 final LR-H1 healthy exact-v4 long-soak acceptance execution exposed a real fail-closed thermodynamic-domain exit at fluid node `outlet` (`v=0.0026153411609661885 m^3/kg`, `u=1615124.4119888516 J/kg`) after the 300 s checkpoint and before 600 s. Diagnostic 1 now proves a real pre-failure primary inventory redistribution inside 0..300 s: outlet mass falls from about 7,504.6 kg to 4,605.0 kg, final-60 s `dm/dt` is about -7.914 kg/s, total node mass slope closes to numerical zero, and production pressure heads imply a channel-return residual near -7.85 kg/s. The immediate owner is therefore the primary branch / reference operating-point balance, while the upstream contribution of authored seed balance versus closed-loop controller bias remains under Diagnostic 2. The envelope and frozen long tolerances must not be widened. See `M10_LR_H1_EQUILIBRIUM_DIAGNOSTIC_PLAN.md` and `M10_FINAL_LONG_FAILURE_DIAGNOSTIC2.md`.

- The project does not yet provide a general full-plant operating-point equilibrium solver. Current operational seeds are authored/versioned initial conditions with bounded deterministic preconditioning, not automatically solved fixed points. `FullPlantSteadyStateCriteria` and supervisory `HoldCurrentOperatingPoint` must not be interpreted as proof of whole-plant equilibrium. The planned M12.0 work adds residual/stability qualification first and a bounded trimmer only if evidence justifies it.
- Domain support is currently fail-closed rather than expressed as a general distance-to-boundary metric. A planned validation-only domain-headroom probe will use the canonical thermodynamic resolver as the support authority rather than duplicating water/steam equations.

## Reactor physics

- The core model is reduced-order rather than a full 3D neutronic/thermal-hydraulic solver.
- Point kinetics and quasi-spatial/group behaviour are educational approximations; they are not a licensing transient-analysis model.
- Xenon, feedback and decay-heat behaviour are deterministic reduced-order models with configured coefficients rather than plant-certified data.
- No detailed fuel failure, channel rupture propagation, graphite damage or severe core-damage mechanics are currently authoritative.

## Turbine / electrical system

- Turbine expansion, losses and valve capacity are reduced-order thermodynamic/mechanical models.
- The grid is an educational infinite-bus/reduced coupling model, not a full electromagnetic transient or multi-machine load-flow solver.
- Electrical protection is reduced-order supervised/delayed logic, not impedance/differential/EMT relay simulation.

## Numerical coupling

- Historical H.30 RQ1 promoted exact desktop `@3` corrected-commit because exact `@2` explicit showed targeted steam-train reverse-flow/shaft-drop discontinuities. Exact `@3` remains immutable replay provenance. Authoritative desktop production is now exact `@4`, which preserves corrected-commit ownership while using the repaired thermodynamic closure.
- `pre-synchronization-grid-loading@2` is retained as an exact explicit compatibility/reference identity, but it is not the supported sustained low-load synchronization journey: the I.5 long diagnostic reproduced late rotor/export instability and reverse-admission steps. Synchronization exact `@3` preserves the @2 physical/control/grid seed and uses corrected-commit for the supported sustained journey. This synchronization version family is independent from desktop exact `@4`.
- Historical H.28 remains `bounded-but-costly` provenance for the old closure. Repaired Stage 4 compares repaired explicit vs repaired corrected on the same thermodynamic closure and passed the original relative ceilings (approximately median wall ratio **0.977**, p95 **0.834**, allocation ratio **1.116**); the validated repaired classification is `bounded-at-or-below-explicit` on that machine-local benchmark. Exact values remain in the corresponding frozen Phase-I/H.28 evidence rather than being treated as human-facing calibration targets.
- Repaired Stage 2 observed zero branch overrides but 3,720 previous-phase holds across 58 trigger steps, including 50 post-startup continuity-active steps. Branch override machinery may now be topologically obsolete, but previous-phase hysteresis remains materially exercised and must not be removed merely because the vapor seam became single-root. Any retirement requires separately scoped evidence after Phase-I closure.
- `DeterministicHybridSemiImplicit` and `FourNodeBranchContinuityShadowIntegrated` are **source-retained historical modes**. They are not production choices, not exact-version compatibility requirements and not current-CI dependencies. I.4 defers physical deletion because historical executable seams still preserve provenance.

## Plant completeness

Still simplified or omitted:

- regenerative feed heating/deaeration/moisture-separator-reheater detail;
- complete emergency core cooling/residual heat removal;
- detailed ventilation/fire/suppression systems;
- detailed structural/mechanical failure progression;
- complete severe-accident chemistry and containment behaviour.

## Recorder / replay evidence

- Full-step M9.1 recording intentionally retains one fingerprinted frame per logical step and can grow substantially during hour-scale sessions. Current v1 has no silent decimation/truncation contract; M11.3 owns measured memory/streaming/chunking work and M11.2 owns any versioned retention-format change.
- `ControlRoomSnapshotFingerprint` v1 is JSON-shape-derived deterministic evidence. M10.9.7.4 added the populated golden anchor and subsequent replay/checkpoint gates preserve it; a future fingerprint v2 requires explicit multi-algorithm compatibility rather than redefining v1.
- `ScenarioRecorder` is observational with respect to plant state/authority but executes synchronously. Its long-term evidence-failure/continue policy is not yet generalized; M11.3 owns that robustness decision.
- `ScenarioChallengeTracker.LifecycleChanged` currently preserves per-step observation-refresh semantics while a challenge is active; M10.9.7.3 suppresses redundant UI work independently through presentation cadence and structural change detection. Any outcome-only notification split is future measured Application hardening.

## HMI / operator model

- The control room is educational rather than a one-to-one reproduction of a specific historical plant.
- Some operator-facing values are intentionally filtered/presented differently from raw solver diagnostics.
- Numerical diagnostics remain engineering evidence and are not automatically exposed as operator controls or predictions.

## Digital I&C, timing and human-system fidelity

- The desktop simulator is not a hard-real-time safety-I&C platform. Simulation semantics use deterministic 10 ms logical time; wall-clock/UI responsiveness is a release-performance property measured separately.
- The current instrumentation fault framework models deterministic signal/component degradation but does not yet model explicit signal age, transport latency, lost updates, inconsistent redundant indications or delayed command-feedback observation. These are planned post-release M13.9 educational extensions.
- Multiple indications or algorithms are not described as safety-grade redundant/diverse merely because they are duplicated. No protection-diversity claim is made without explicit shared-dependency and functional-diversity analysis.
- The current desktop workspaces may still exhibit bounded keyhole/information-load tradeoffs inherent in a single-window F1–F8 architecture. Critical protection/authority/quality context is required to remain persistently understandable; deeper anti-keyhole and part-task human-system work is planned for M13.9.
- .NET, Avalonia and other dependencies are normal commercial/open-source software dependencies. M11 applies proportional version/configuration/package assurance; the project does not claim nuclear-grade COTS qualification.
- No quantitative software failure probability or nuclear safety reliability figure is claimed. Digital-I&C hazards are handled through deterministic contracts, tests, representative tasks and explicit limitations.

## Severe incidents

Faults, leaks/LOCA-class scenarios, blackout-class scenarios, trips and post-incident analysis exist within the currently modeled physics. The simulator must not be described as a general severe-accident, fire or explosion simulator until explicit persistent-damage owners and validated consequence models exist.

## Validation interpretation

A green regression gate means the current reduced-order contract is internally consistent for the tested domain. It does not imply industrial accuracy outside that domain.


## Desktop host / session-save integrity

The M10.9.7.3 Hotfix 2 REV2 host/session-integrity corrections and subsequent timeline/archive gates are validated: expected archive/step failures cross an explicit desktop boundary, session replacement avoids destructive truncate-first semantics, and M10.9.7.4/8.4 replay/checkpoint evidence is green. Current candidate/validation status belongs only in [`PROJECT.md`](PROJECT.md).

These validated boundaries are not reasons to change physics, challenge/scoring semantics or archive schema. UI-thread responsiveness, notification fan-out, long-session memory/recorder cost, stable-ID command-target selection and `MainWindowViewModel` decomposition remain M11.3/M13 work as described in [`DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md`](DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md).

- M10.9.8 manual acceptance intentionally adds no general challenge launcher or manual-only fault injector; injected instrumentation/fault combinations remain deterministic validation compositions owned by automated M10.9.8.3/8.4 evidence until a separately justified user-facing launcher is designed.
