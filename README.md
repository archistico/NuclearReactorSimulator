# Nuclear Reactor Simulator

Educational full-plant nuclear reactor simulator built with **C# / .NET 10 / Avalonia**.

The project models a reduced RBMK-like plant as a deterministic, headless-testable simulation with an operator-facing desktop control room. The model is intentionally educational: it aims for internally consistent plant behaviour, conservation, replayability and explicit limitations rather than industrial licensing-grade fidelity.

## Current project state

**Authoritative validated baseline:** `M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening`.

**Current candidate:** `M10.9.4.1-H.30 Requalification 1 — Production Policy Re-review after I.3 Continuity Evidence`.

The original H.30 decision was `OPT-IN ONLY`: exact v2 `ExplicitCommittedState` remained the desktop production default and exact v3 corrected-commit remained qualified opt-in because H.28 classified its runtime cost as `bounded-but-costly`.

Subsequent validated Phase-I evidence changed that trade-off:

- exact v2: 338 generation-drop steps in the 100 s / 10 ms comparison, all 338 coincident one-for-one with reverse flow in the targeted stop/control/admission train;
- exact v3: 0 generation drops and 0 targeted reverse-flow steps in the same comparison;
- exact v3: 300 s / 30,000-step healthy reference requalification with 0 generation-health violations, 0 targeted reverse-flow violations, 0 rollback/fallback/unsafe commits and deterministic repeat.

H.30 Requalification 1 therefore **proposes `ACTIVATE`**: exact v3 becomes the authoritative desktop production default while exact v2 remains the exact-version fail-closed rollback/reference path. This proposal is not authoritative until local build, ordinary tests and the focused H.30 RQ1 audit all pass.

I.3 tolerance budgets remain **unfrozen** until the production-policy re-review is validated and the reference baseline is rerun under the resulting authoritative policy.

## Build and run

Requirements:

- .NET SDK 10 matching `global.json`;
- Windows, Linux or macOS supported by Avalonia and the selected .NET runtime.

```bat

dotnet restore
dotnet build
dotnet test
```

Run the desktop application with the normal App project command used by your environment/IDE.

For the current H.30 RQ1 candidate:

```bat
scripts\run-h30-rq1-production-policy-rereview-audit.cmd
```

CI entry points are under `eng\`:

```text
eng\ci-ordinary.cmd          ordinary build/test + current evidence
eng\ci-current-evidence.cmd current lightweight evidence gates
eng\ci-long.cmd              scheduled/manual long-running gates
```

## Runtime contract

The core rules that should remain stable across future milestones are:

- deterministic external fixed step: **10 ms**;
- simulation logic independent from UI refresh and wall-clock cadence;
- immutable/copy-on-write state across kernel steps;
- staged committed-state plant solving;
- each conserved inventory integrated exactly once per plant step;
- mass/energy ownership explicit and auditable;
- runtime faults and corrected-candidate refusal fail closed;
- scenario/save/replay identities are exact-versioned and never silently reinterpreted;
- GUI contains no reactor-physics calculations;
- automated tests can run the simulation headlessly.

## Current desktop hydraulic versions

| Exact version | Hydraulic mode | Role in H.30 RQ1 candidate |
| --- | --- | --- |
| `integrated-operations-desktop-stable@1` | explicit historical | compatibility retained |
| `integrated-operations-desktop-stable@2` | `ExplicitCommittedState` | rollback/reference |
| `integrated-operations-desktop-stable@3` | `FourNodeBranchContinuityCorrectedCommitOptIn` | proposed authoritative default |

The v3 mode name retains its historical H.29/H.22 lineage even if H.30 RQ1 promotes it to production default. Renaming the numerical enum is not part of this requalification. H.30 RQ1 uses a new production scenario identity, `integrated-normal-operations-training-h30-rq1-production`, so the historical H.29 candidate scenario is not repurposed.

## Main capabilities

The validated codebase includes, among other areas:

- strongly typed physical quantities and simplified water/steam thermodynamics;
- point kinetics, delayed neutron precursors, decay heat, temperature/void/xenon feedback;
- primary circulation, steam drums, main-steam network, turbine, condenser, feedwater and electrical/grid coupling;
- protection/interlock/trip logic and deterministic fault/scenario infrastructure;
- operator control-room UI, alarms, trends, mimic/schematics and operator-computer workflows;
- recorder, checkpoints, exact-version save/replay and deterministic replay verification;
- long-running gameplay, operational-envelope, conservation, protection and performance audit tiers.

This list is intentionally high-level. Detailed subsystem contracts live under `docs/`.

## Documentation

Start with:

- `docs/PROJECT_STATUS.md` — current authoritative/candidate state;
- `docs/PROJECT_HANDOFF.md` — exact continuation point for development;
- `docs/ROADMAP.md` — remaining Phase-I work and M10.9.5–M10.9.8 direction;
- `docs/KNOWN_MODEL_LIMITATIONS.md` — current limitations only;
- `docs/current/` — documents for the active candidate;
- `docs/history/` — completed/superseded milestone records retained for provenance;
- `docs/adr/` — architectural decision records;
- `docs/milestones/` — milestone summaries;
- `docs/README.md` — documentation map.

The repository deliberately separates **current documentation** from **historical evidence**. Do not determine the current production policy by reading an old H/I milestone note under `docs/history/`.

## Safety and scope

This is an educational simulator, not a reactor design, operations, licensing or safety-analysis tool. Thermodynamic properties, component models, spatial physics, protection and severe-incident behaviour are reduced-order approximations with explicit documented limitations.
