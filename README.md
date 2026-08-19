# Nuclear Reactor Simulator

Educational full-plant nuclear reactor simulator built with **C# / .NET 10 / Avalonia**.

The project models a reduced RBMK-like plant as a deterministic, headless-testable simulation with an operator-facing desktop control room. The model is intentionally educational: it aims for internally consistent plant behaviour, conservation, replayability and explicit limitations rather than industrial licensing-grade fidelity.

## Current project state

**Authoritative validated production policy:** `M10.9.4.1-H.30 Requalification 1` — `ACTIVATE`.

Exact v3 `FourNodeBranchContinuityCorrectedCommitOptIn` is the authoritative desktop production default. Exact v2 `ExplicitCommittedState` remains the fail-closed rollback/reference and compatibility path. The fixed timestep remains 10 ms and the corrected path retains H.28's `bounded-but-costly` performance classification.

**Current Phase-I candidate:** `M10.9.4.1-I.3 Hotfix 2 — Authoritative Production Reference Trajectory, Conservation/Inventory & Tolerance Baseline / Compact Frozen Evidence Contracts`.

I.3 reruns the healthy 300-second reference under the production selector, checks generation/steam-train continuity at every 10 ms step, records one-second conservation/inventory samples, and derives the first 19 versioned internal regression budgets from the final 60 seconds. No runtime physics is retuned to fit those budgets.

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

For the current I.3 candidate:

```bat
scripts\run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd
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

| Exact version | Hydraulic mode | Current role |
| --- | --- | --- |
| `integrated-operations-desktop-stable@1` | explicit historical | compatibility retained |
| `integrated-operations-desktop-stable@2` | `ExplicitCommittedState` | rollback/reference |
| `integrated-operations-desktop-stable@3` | `FourNodeBranchContinuityCorrectedCommitOptIn` | authoritative production default |

The v3 mode name retains its historical H.29/H.22 lineage even though H.30 RQ1 promotes it to production default. Renaming the numerical enum is not part of the activated policy. H.30 RQ1 uses a new production scenario identity, `integrated-normal-operations-training-h30-rq1-production`, so the historical H.29 candidate scenario is not repurposed.

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
- `docs/current/` — only documents for the active candidate;
- `docs/history/` — completed/superseded milestone records retained for provenance;
- `docs/adr/` — architectural decision records;
- `docs/milestones/` — milestone summaries;
- `docs/README.md` — documentation map.

The repository deliberately separates **current documentation** from **historical evidence**. Large generated audit payloads under `tests/.../Gameplay/Evidence` and `artifacts/` are local/separate and are excluded from candidate ZIPs. Ordinary tests use the bounded immutable store under `eng/frozen-evidence/ordinary`; intentionally omitted multi-megabyte historical traces are represented only by canonical hashes in `eng/frozen-evidence/large-payload-manifest.csv`. Decision provenance remains under `eng/evidence-manifests/`. Do not determine current policy from old notes under `docs/history/`.

## Safety and scope

This is an educational simulator, not a reactor design, operations, licensing or safety-analysis tool. Thermodynamic properties, component models, spatial physics, protection and severe-incident behaviour are reduced-order approximations with explicit documented limitations.
