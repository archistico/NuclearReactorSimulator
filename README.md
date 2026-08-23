> **M10 final active investigation:** Diagnostic 5 completed PASS and supplied the whole-cycle owner census. Diagnostic 6 is now the active exact-v6 analytical whole-cycle equilibrium candidate: it closes the stationary primary loop at 100 kg/s, the 5 MWe turbine-generator/steam path, condenser and feedwater hydraulics from unchanged model equations. Exact-v4 remains production; exact-v5 remains failed diagnostic evidence; production activation and replacement long remain unauthorized. See `docs/M10_FINAL_LONG_FAILURE_DIAGNOSTIC6.md`.


> **Hotfix 1:** the long preflight ignores generated `bin/` and `obj/` build output when checking the frozen `src/` and `tests/` manifests, so the gate remains valid even if `dotnet build` or `dotnet test` was run before the long script.

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
