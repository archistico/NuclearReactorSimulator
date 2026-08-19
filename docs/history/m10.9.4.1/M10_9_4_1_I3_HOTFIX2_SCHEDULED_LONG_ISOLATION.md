# M10.9.4.1-I.3 Hotfix 2 — Scheduled-Long Isolation & Shaft-Drop Diagnostic

Status: **CANDIDATE**. Latest validated baseline: **I.2**.

## Purpose

Hotfix 1 showed that the 300-second I.3 collector can be executed by the complete test run on the current runner despite `Explicit = true`. That contradicts the I.2 tiering contract, where I.3 belongs to scheduled/manual long evidence.

Hotfix 2 adds a fail-closed environment opt-in. The collector executes only when `NRS_I3_LONG_AUDIT=1`; the focused I.3 script sets this variable immediately before invoking the explicit category. Ordinary test runs therefore do not perform the 300-second journey.

## Numerical/runtime scope

None. The shaft-power health floor remains `> 4.5 MW`; all Hotfix 1 full-horizon diagnostic outputs remain unchanged. No solver, plant coefficient, production selector, persistence identity, H.9/H.20/H.22 contract or 10 ms timestep is changed.

## Validation

1. `dotnet build`
2. `dotnet test` — must be green and must not run the 300-second collector.
3. `scripts\run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd` — explicitly opts in and performs the full collector.

If step 3 is red on generation health, retain the generated `03`, `06` and `07` CSV artifacts and diagnose the runtime before changing any tolerance budget.
