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

Generated audit CSV/TXT payloads and `tests/.../Gameplay/Evidence` are deliberately excluded from candidate ZIPs. Compact frozen prerequisites required by ordinary/current validation live under `eng/frozen-evidence/ordinary`. Direct ordinary payloads are capped at 1 MiB; larger immutable raw evidence is kept byte-for-byte in compressed packs under `eng/frozen-evidence/archive` and authenticated by `eng/frozen-evidence/large-payload-manifest.csv`. Historical gates that need an externalized raw payload must explicitly restore it with `.\scripts\restore-frozen-evidence-large-payloads.ps1` and compact again afterward with `.\scripts\compact-frozen-evidence-ordinary.ps1`.

## Safety and scope

This is an educational simulator, not a reactor design, operations, licensing or safety-analysis tool. Thermodynamic properties, component models, spatial physics, protection and severe-incident behaviour are reduced-order approximations with documented limitations.

### Historical M10 Branch A3 checkpoint (superseded)
FDPC2 Planning 1 returned `PASS-AS-AUTHORED`. At that historical checkpoint the only live executable gate was `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2`, evidence-only under `AMBIENT-UNSET` on the frozen A2 host and power scheme. FDPC1 remains negative historical evidence. RP1C selection and all production/runtime authority remain blocked pending returned FDPC2 adjudication.

### Historical M10 VR2 checkpoint (superseded)

FDPC2 returned green. At that historical checkpoint the live gate was planning-only **RP1C Selection Planning 1**; returned Selection Planning 1 and Selection 1 now supersede that checkpoint.

### Historical M10 RP1C checkpoint (superseded)

Selection Planning 1 returned `PASS-AS-AUTHORED`. At that checkpoint the live gate was decision-only `RP1C-ENGINEERING-REPAIR-SELECTION1`, with authored `SELECT-C4` and mandatory preserved `SELECT-NONE`. Returned Selection 1 supersedes that checkpoint.

### Historical R1 Implementation Planning 1 candidate (superseded)

RP1C Selection 1 returned `SELECT-C4`. The repository now contains a planning-only R1 contract that preserves existing production closure modes and exact-v9 while freezing a future explicit mode `ReferenceConsistentTabulatedInverseDomain = 2`. The reviewed contract also freezes the explicit Simulation project-file resource boundary, `NRSVR2C4` v1 payload format/load policy, dedicated mode-2 dispatch and the exact 1,679-state + 288-hydraulic equivalence corpus. No production source or test change is included in this candidate.

### Historical R1 Selected C4 Opt-In Closure Implementation 1 candidate (superseded)

Returned R1 Implementation Planning 1 was `PASS-AS-AUTHORED`; R1 implementation later returned/adjudicated PASS and R2 focused qualification is now also returned/adjudicated PASS.

