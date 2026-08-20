# Nuclear Reactor Simulator

Educational full-plant nuclear reactor simulator built with **C# / .NET 10 / Avalonia**. The project models a reduced RBMK-like plant as a deterministic, headless-testable simulation with an operator-facing desktop control room.

It targets internally consistent educational behaviour, conservation, replayability and explicit model limits rather than industrial licensing-grade fidelity.

## Current work

Current candidate: **M10.9.4.1-I.5 REV1 Hotfix 17.1 — Final Repaired-v4 Phase-I Closure / Preflight Documentation Alignment**.

Hotfix 16.2 is locally validated: authoritative desktop production is exact `integrated-operations-desktop-stable@4`, with `CorrelationConsistentInverseDomain` thermodynamics and `FourNodeBranchContinuityCorrectedCommitOptIn` hydraulics at the unchanged 10 ms fixed step. Historical exact @3 and rollback exact @2 remain immutable.

Hotfix 17 adds no new repair stage. It realigns current CI so H.30 RQ1, I.3 exact-v3 and I.4 are frozen historical provenance, adds the final authoritative exact-v4 300-second production-reference requalification against the unchanged 19 I.3 budgets, and makes the existing cumulative closure command run the final repaired-v4 long matrix. Hotfix 17.1 changes no runtime/test/CI logic: it completes the static preflight of namespaces, filter targets, script targets, budget hashes and evidence paths, and realigns the current technical documentation to authoritative exact @4.

The detailed checkpoint and final validation sequence live in:

**[`docs/PROJECT.md`](docs/PROJECT.md)**

## Build and test

```bat
dotnet restore
dotnet build
dotnet test
```

For the final Phase-I closure candidate, first run the short compile/ordinary preflight, then launch the cumulative closure:

```bat
dotnet build
dotnet test
scripts\run-m10941-cumulative-closure-audit.cmd
```

The cumulative command intentionally reruns ordinary/current evidence and the complete scheduled-long matrix. It can take multiple hours. If it is green, M10.9.4.1 / Phase I is closed and M10.9.5 is unblocked.

## Core runtime principles

- deterministic external fixed timestep;
- simulation logic independent from UI refresh/wall-clock cadence;
- explicit mass/energy ownership;
- fail-closed numerical correction/rollback contracts;
- exact-version scenario/save/replay identities are never silently reinterpreted;
- GUI contains no reactor-physics calculations;
- headless automated validation remains authoritative.

## Documentation

Start at **[`docs/README.md`](docs/README.md)**.

The current documentation is intentionally split by responsibility rather than milestone chronology:

- `docs/PROJECT.md` — **only** current status/handoff/active validation source;
- `docs/ROADMAP.md` — future milestones and sequencing;
- `docs/KNOWN_MODEL_LIMITATIONS.md` — unresolved model limitations;
- `docs/ARCHITECTURE.md` — stable architecture and ownership;
- subsystem documents — detailed technical reference;
- `docs/history/` — superseded milestone/hotfix chronology;
- `docs/adr/` — architectural decisions.

Generated audit CSV/TXT payloads and `tests/.../Gameplay/Evidence` are deliberately excluded from candidate ZIPs. Compact frozen prerequisites required by ordinary tests live under `eng/frozen-evidence/`.

## Safety and scope

This is an educational simulator, not a reactor design, operations, licensing or safety-analysis tool. Thermodynamic properties, component models, spatial physics, protection and severe-incident behaviour are reduced-order approximations with documented limitations.
