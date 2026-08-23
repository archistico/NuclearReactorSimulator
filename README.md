> **M10 final active investigation:** Diagnostic 10 original candidate compiled but the ordinary suite stopped RED at 1/1480 failures in the new moisture-drain regression. The failure is test-only: the assertion modeled the turbine stage sink as the sole `turbine-inlet` owner and omitted the canonical admission-valve hydraulic inflow. Diagnostic 10 Hotfix 1 aligns that regression with the exact net node balance; `src/`, exact-v8 runtime semantics and all physical coefficients remain unchanged. Exact-v4 remains production; production activation and replacement long are not authorized. See `docs/M10_FINAL_LONG_FAILURE_DIAGNOSTIC10.md`.


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
