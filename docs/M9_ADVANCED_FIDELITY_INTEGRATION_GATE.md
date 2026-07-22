# M9 Advanced Fidelity Integration Gate

## Final validation outcome

**VALIDATED / M9 GATE COMPLETE.** M9.7 hotfix 5 compiled and all **760/760 automated tests passed**, including both 6,000-step / 60-second endurance paths. The remaining manual center-workspace layout issue was corrected in the user-supplied `MainWindow.axaml`, which is integrated as the authoritative final M9.7 GUI layout baseline. M10.1 may proceed.


## Purpose

M9.7 is a verification boundary, not a new physics milestone. M9.1–M9.6 introduced several orthogonal capabilities that must remain compatible when composed:

```text
M9.1 recorder / checkpoint / replay
            +
M9.2 post-incident analysis
            +
M9.3 iodine/xenon operational integration
            +
M9.4 quasi-spatial feedback refinement
            +
M9.5 historical provenance/fidelity declarations
            +
M9.6 calibration/reference validation
            ↓
M9.7 integrated evidence gate
```

The gate exists to detect cross-feature regressions that isolated milestone tests may miss.

## Ownership invariants

M9.7 does not alter ownership:

- M2 remains the owner of reactivity, point kinetics, fission/decay power and iodine/xenon;
- M3 remains the owner of primary-circuit inventories and canonical aggregated core/channel topology;
- M4 remains the owner of secondary-cycle/generator physics;
- M5 remains the owner of measured instrumentation, automatic control, protection and alarms;
- M9.1 remains the recorder/checkpoint/replay authority;
- M9.2 remains observational evidence analysis only;
- M9.4 may refine the zonal feedback/power-shape representation but does not create a second neutron population;
- M9.5 metadata cannot force physical outcomes;
- M9.6 validation observes explicit evidence and does not tune physics automatically.

## Cross-feature reactivity composition

When xenon and quasi-spatial feedback are simultaneously enabled, the intended composition is:

```text
committed rod reactivity
        +
explicit external distinct non-rod contribution
        +
committed M2.8 xenon reactivity
        +
M9.4 power-weighted quasi-spatial feedback
        ↓
one global total reactivity
        ↓
one canonical point-kinetics solve
```

The same physical feedback must never also be hidden inside `ExternalNonRodReactivity`.

## Replay/analysis/reference invariant

For a deterministic recorded scenario:

- exact scenario and initial-condition identity/version are preserved;
- every recorded fixed-step fingerprint is reproduced by replay;
- event sequence is reproduced exactly;
- checkpoint seek remains replay-backed and fingerprint-verified;
- M9.2 analysis observes the immutable recording only;
- M9.6 metric extraction over original and replayed snapshots produces identical evidence for identical frames.

Host RUN/PAUSE presentation mode remains normalized out of replay identity as established by M9.1.

## Historical-fidelity invariant

Passing M9.7 means only that the implemented M9 seams are mutually compatible and their declared internal reference baselines remain green.

It does **not** mean:

- historical reconstruction accuracy;
- licensing-grade validation;
- externally certified calibration;
- proof of causal interpretation;
- high-fidelity spatial neutronics.

Historical-inspired scenarios must continue to declare sources, claim kinds, required model capabilities and deliberate non-claims explicitly.

## GUI boundary

Before M10 expands the human-machine interface, M9.7 freezes the current GUI invariants:

- Views/ViewModels own no physics;
- commands remain typed intents through canonical dispatchers;
- snapshot publication drives presentation updates;
- missing values stay unavailable;
- alarm acknowledgement/reset remains distinct from physical protection reset;
- command availability never replaces runtime fail-closed validation;
- the current desktop default session is validated manually only through capabilities actually exposed by that UI.

Automated App tests may instantiate additional real versioned runtimes, including M9.3 xenon seeds, to verify the presentation boundary without pretending those scenarios are already selectable from the current desktop UI.

### Hotfix 1 — first manual desktop gate findings

The first M9.7 manual GUI pass exposed four integration defects that isolated physics/UI tests had not caught:

- the M9.7 descriptor regression still asserted the previous M9.6 candidate label;
- the desktop composition still loaded the validated M7.7 v1 low-load seed, whose deliberately simplified turbine steam-path initialization could leave `control-out` with too little vapor inventory for sustained desktop stepping;
- `Run` changed host mode but the Avalonia desktop had no cooperative host pump calling `AdvanceRunning`, so logical time did not advance continuously;
- operator button hit surfaces/cursor feedback and center-content/footer clipping were not strong enough for reliable manual operation.

The hotfix preserves ownership and versioning:

- M7.6/M7.7 v1 identities and replay semantics remain untouched; the desktop uses a new explicitly versioned M9.7 integration seed whose turbine steam path is initialized consistently with the upstream steam-space condition;
- a bounded App-layer `DispatcherTimer` requests deterministic fixed-step batches through `ControlRoomRuntimeCoordinator`; wall-clock cadence schedules work only and never changes the fixed simulation timestep or owns physics;
- deterministic step failure pauses host execution and reports the failure instead of looping exceptions or mutating physical/protection state;
- RUN state and logical-step progress are visible beside the host controls;
- operator push-buttons expose a full rectangular hit surface and pointer feedback;
- the main content viewport is clipped above the footer and retains enough bottom scroll extent to reveal the final architecture text completely.

These items are covered by automated regression tests plus the final manual M9 checklist.

## Exit condition

M9 closes only when the complete automated suite and the final manual GUI checklist are green. The next phase is M10 `Operator Computer, Supervisory Automation & Human-Machine Integration`; final product release hardening remains M11.


### Hotfix 3 — second manual gate hardening

After M9.7 hotfix 2 passed local build and the complete automated suite, manual validation still exposed bidirectional center-workspace clipping, no explicit whole-session reset and a block around logical step 3111. Hotfix 3 keeps M9.7 as an integration gate rather than adding physics: center padding/extent is moved inside the scroll content with automatic horizontal and vertical access; `Reset session` reconstructs the exact versioned desktop composition; and the desktop endurance gate is extended to 6,000 fixed steps / 60 simulated seconds. The desktop-only candidate seed uses a balanced 5 MWe low-load request and finite 0.1 MW condenser cooling. Validated M7 identities and factory defaults remain unchanged; thermodynamic boundary robustness is handled by the canonical closure rather than an inflated seed-only liquid margin.

### Hotfix 4 — saturation-boundary closure robustness

The 6,000-step gate exposed a numerical closure gap in `SimplifiedWaterSteamThermodynamicModel`: a full-range fixed-grid saturated-mixture scan can miss a valid root when the specific-volume admissibility interval ends between samples near quality 0 or 1. The failure was reproduced from the exact `drum` and `exhaust` conserved states.

Hotfix 4 preserves all previously successful resolver branches and adds only a final deterministic boundary-aware saturated-mixture search before `WaterSteamStateOutOfRangeException`. It brackets the physically admissible saturation-temperature interval for the actual specific volume and uses the existing bisection/model equations. This is a numerical root-search correction, not a widened envelope or a scripted stability outcome. The 60-second direct-session and real desktop-pump endurance tests remain mandatory.

### Hotfix 5 — superheated-onset closure robustness

Revalidation of hotfix 4 progressed much farther through the endurance run but exposed a later `exhaust` state at `v=65.477888248812704 m^3/kg`, `u=2434381.9782870663 J/kg`. Direct evaluation of the existing M1.7 equations shows a valid `SuperheatedVapor` root at roughly 17.907 °C / 2.052 kPa. The root lies immediately above the first admissible superheated temperature, between coarse 512-segment scan samples, so the original full-range scan can miss its sign change for the same numerical reason that hotfix 4 addressed on the saturation branch.

Hotfix 5 adds the symmetric final fallback for the superheated branch: determine the exact contiguous admissible temperature interval for the actual specific volume, include its boundary endpoints, then reuse the existing deterministic superheated residual and bisection. A dedicated negative regression also proves that a genuine gap between the simplified saturation and superheated correlations remains fail-closed when neither branch contains a root. The 6,000-step / 60-second direct-session and real desktop-pump gates remain unchanged.
