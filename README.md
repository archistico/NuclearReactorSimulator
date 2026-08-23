> **M10 Final replacement-long failure Diagnostic 4:** Diagnostic 3 is execution PASS. Direct breaker-closed SPEED authority and simple valve preload do not improve the 5→10 MWe loss-of-synchronism path, and exact-v4 reproduces the same failure family. The active evidence-only candidate now discriminates electrical request granularity from missing slow reactor/steam energy support before any production repair. Run `scripts\run-m10-final-replacement-long-failure-diagnostic4.cmd` and return the complete artifact folder.

# Nuclear Reactor Simulator

Educational full-plant nuclear reactor simulator built with **C# / .NET 10 / Avalonia**. The project models a reduced RBMK-like plant as a deterministic, headless-testable simulation with an operator-facing desktop control room.

It targets internally consistent educational behaviour, conservation, replayability and explicit model limits rather than industrial licensing-grade fidelity.

## Current work

The authoritative current checkpoint, active candidate, superseded chain and candidate-specific validation commands live only in:

**[`docs/PROJECT.md`](docs/PROJECT.md)**

README intentionally does not duplicate milestone status. `docs/ROADMAP.md` contains future sequencing only, while detailed milestone contracts live under `docs/milestones/`.

## Build and test

```bat
dotnet restore
dotnet build
dotnet test
```

For the active candidate, use the focused validation command recorded in `docs/PROJECT.md`; README deliberately keeps only the generic build/test entry point.

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
