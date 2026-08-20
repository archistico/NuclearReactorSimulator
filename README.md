# Nuclear Reactor Simulator

**Current candidate:** M10.9.4.1-I.4 Hotfix 2 — Canonical Frozen-Evidence Contract Alignment

Educational full-plant nuclear reactor simulator built with **C# / .NET 10 / Avalonia**.

The project models a reduced RBMK-like plant as a deterministic, headless-testable simulation with an operator-facing desktop control room. It targets internally consistent educational behaviour, conservation, replayability and explicit model limits rather than industrial licensing-grade fidelity.

## Current project state

**Validated production policy:** `M10.9.4.1-H.30 Requalification 1 — ACTIVATE`.

Exact v3 `FourNodeBranchContinuityCorrectedCommitOptIn` is the authoritative desktop production default. Exact v2 `ExplicitCommittedState` remains fail-closed rollback/reference. The fixed timestep remains 10 ms and H.28 still classifies the corrected path `bounded-but-costly`.

**Validated Phase-I reference baseline:** `M10.9.4.1-I.3 Hotfix 2`.

The authoritative v3 reference completed 300 s / 30,000 steps with zero generation-health violations, zero targeted stop/control/admission reverse-flow violations, clean corrected telemetry and deterministic repeat. Seven final-window slope observations and 19 internal regression budgets are frozen.

**Current candidate:** `M10.9.4.1-I.4 — Known Limitations & Legacy Retirement Review`.

I.4 records the remaining reference drifts as known limitations and reviews the H.5 hybrid / H.21 shadow-integrated historical numerical modes. Neither mode is a production, exact-version or current-CI dependency; physical source removal is deferred because executable historical seams still compile against them.

## Build and validate

```bat
dotnet restore
dotnet build
dotnet test
scripts\run-phase-i-known-limitations-legacy-retirement-review-audit.cmd
```

CI entry points:

```text
eng\ci-ordinary.cmd
eng\ci-current-evidence.cmd
eng\ci-long.cmd
```

## Runtime contract

- deterministic external fixed step: **10 ms**;
- simulation logic independent from UI refresh/wall-clock cadence;
- immutable/copy-on-write state across kernel steps;
- each conserved inventory integrated once per plant step;
- mass/energy ownership explicit and auditable;
- corrected-candidate refusal fails closed;
- exact-version scenario/save/replay identities are never silently reinterpreted;
- GUI contains no reactor-physics calculations;
- headless automated validation remains authoritative.

## Current desktop hydraulic versions

| Exact version | Hydraulic mode | Current role |
| --- | --- | --- |
| `integrated-operations-desktop-stable@1` | historical explicit | compatibility retained |
| `integrated-operations-desktop-stable@2` | `ExplicitCommittedState` | rollback/reference |
| `integrated-operations-desktop-stable@3` | `FourNodeBranchContinuityCorrectedCommitOptIn` | authoritative production default |

The v3 enum name retains historical H.22/H.29 lineage; H.30 RQ1 activated the already-qualified path without renaming or retuning it.

## Documentation

Start with:

- `docs/PROJECT_STATUS.md` — authoritative checkpoint;
- `docs/PROJECT_HANDOFF.md` — continuation instructions;
- `docs/ROADMAP.md` — remaining Phase-I work;
- `docs/KNOWN_MODEL_LIMITATIONS.md` — current limitations only;
- `docs/current/` — active candidate documents only;
- `docs/history/` — completed/superseded chronology;
- `docs/adr/` — architectural decisions;
- `docs/milestones/` — milestone summaries.

Large generated audit payloads under `tests/.../Gameplay/Evidence` and `artifacts/` are deliberately excluded from candidate ZIPs. Ordinary tests use compact immutable prerequisites under `eng/frozen-evidence/ordinary`; large historical traces used only for identity checks are represented by hashes in `eng/frozen-evidence/large-payload-manifest.csv`.

## Safety and scope

This is an educational simulator, not a reactor design, operations, licensing or safety-analysis tool. Thermodynamic properties, component models, spatial physics, protection and severe-incident behaviour are reduced-order approximations with explicit documented limitations.
