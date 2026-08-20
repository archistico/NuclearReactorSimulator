# Nuclear Reactor Simulator

Educational full-plant nuclear reactor simulator built with **C# / .NET 10 / Avalonia**. The project models a reduced RBMK-like plant as a deterministic, headless-testable simulation with an operator-facing desktop control room.

It targets internally consistent educational behaviour, conservation, replayability and explicit model limits rather than industrial licensing-grade fidelity.

## Current work

M10.9.4.1 / Phase I is **validated and closed**. Authoritative desktop production is exact `integrated-operations-desktop-stable@4` with `CorrelationConsistentInverseDomain` thermodynamics and `FourNodeBranchContinuityCorrectedCommitOptIn` hydraulics at the unchanged 10 ms fixed step. Historical desktop exact @3, rollback exact @2 and synchronization exact @3 remain immutable in their respective roles.

Current candidate: **M10.9.5.3 Hotfix 2 — COMMANDS Context Inspector XAML-contract / schematic-focus fix**.

M10.9.5.1 and M10.9.5.2 are validated. The initial M10.9.5.3 candidate exposed a ViewModel API-name mismatch, fixed by Hotfix 1. That package then compiled, but the ordinary App suite exposed one stale XAML-contract expectation: the presentation-only mimic focus is intentionally bound `Mode=OneWay`. Hotfix 2 aligns the test with that contract and removes a fallback highlight that could visually imply a schematic target for a non-graphical dependency step. Graphical element references focus directly; connection references use an explicitly labelled source-element proxy; command-target/published-state steps show no highlight. Selection never dispatches; ENTER/EXECUTE remains the only dispatch boundary. No new topology, predictive physics, protection/control ownership or plant-state mutation is introduced.

The authoritative checkpoint and validation sequence live in:

**[`docs/PROJECT.md`](docs/PROJECT.md)**

## Build and test

```bat
dotnet restore
dotnet build
dotnet test
```

For the active M10.9.5.3 candidate:

```bat
dotnet build
dotnet test
scripts\run-m1095-command-context-inspector-schematic-audit.cmd
```

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
