# ADR 0190 — Recompute the authored whole-cycle operating point after turbine moisture separation

## Status

Accepted / Diagnostic 11 Hotfix 2 returned 600 s evidence qualifies exact-v9 as the post-moisture whole-cycle operating point. Production activation remains a separate gate.

## Context

Diagnostic 10 Hotfix 1 validates ADR 0189 moisture-drain ownership but shows that exact-v8 still uses the pre-drain secondary mass/energy root. Its structural owners are stable, yet late electrical export is about 4.868 MWe and net/stored energy is about +0.255 MW.

## Decision

Create a distinct exact-version `integrated-operations-desktop-stable@9` that preserves exact-v8 runtime semantics and recomputes only the authored whole-cycle state from the unchanged equations. The new root treats 13.0280018984 kg/s as work-producing vapor, solves total admission and explicit drain flow together with the turbine/condenser equations, closes the hotwell/feedwater liquid loop including drain enthalpy, and recomputes the fission/solid thermal seed from the full external first-law balance.

Exact-v4 remains production. Exact-v8 remains immutable Diagnostic-10 evidence. No physical coefficient, controller gain, thermodynamic envelope or conservation tolerance changes.

## Consequences

Diagnostic 11 Hotfix 2 satisfies the required 600 s qualification: stable ~5 MWe operation, primary ~100 kg/s, bounded inventories and thermal state, negligible governor/turbine-inlet drift, zero trip/rollback events and conservative stage/full-cycle energy closure. Exact-v9 may therefore enter a separately gated production-activation candidate. The authoritative default and replacement long remain separate decisions.
