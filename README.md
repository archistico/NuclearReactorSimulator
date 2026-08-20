# Nuclear Reactor Simulator

Educational full-plant nuclear reactor simulator built with **C# / .NET 10 / Avalonia**. The project models a reduced RBMK-like plant as a deterministic, headless-testable simulation with an operator-facing desktop control room.

It targets internally consistent educational behaviour, conservation, replayability and explicit model limits rather than industrial licensing-grade fidelity.

## Current work

M10.9.4.1 / Phase I is **validated and closed**. Authoritative desktop production is exact `integrated-operations-desktop-stable@4` with `CorrelationConsistentInverseDomain` thermodynamics and `FourNodeBranchContinuityCorrectedCommitOptIn` hydraulics at the unchanged 10 ms fixed step. Historical desktop exact @3, rollback exact @2 and synchronization exact @3 remain immutable in their respective roles.

M10.9.5.1, M10.9.5.2 and **M10.9.5.3 Hotfix 2 are validated**.

Current candidate: **M10.9.5.4 — Observed Response Evidence**.

F4 COMMANDS now keeps three semantics visibly separate: authored `DIRECT EFFECT`, qualitative `EXPECTED INFLUENCE`, and post-dispatch `OBSERVED RESPONSE`. M10.9.5.4 samples the already-authored monitor set at the dispatch boundary and through a bounded 500-logical-step window, displaying actual baseline/latest values and delta/direction when meaningful. Rejected commands show feedback but no fictional plant effects. The evidence is presentation-only, JsonIgnored for compatibility, and never becomes generic command success/failure or proof of causality.

The authoritative checkpoint and validation sequence live in:

**[`docs/PROJECT.md`](docs/PROJECT.md)**

## Build and test

```bat
dotnet restore
dotnet build
dotnet test
```

For the active M10.9.5.4 candidate:

```bat
dotnet build
dotnet test
scripts\run-m1095-command-observed-response-audit.cmd
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
