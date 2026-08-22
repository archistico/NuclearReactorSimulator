# M10.9.8.3 — Degraded Measurement / Fault / Protection / Takeover Matrix

## Status

**CANDIDATE / NOT VALIDATED.** Stacked exclusively on **M10.9.8.2 Hotfix 1 REV5 VALIDATED**. This milestone adds automated integration/evidence only: no production runtime source, Simulation physics/coefficient, challenge/scoring/protection owner, archive schema, fingerprint algorithm, plant-command authority, production scenario registration or new fault type.

## Purpose

M10.9.8.3 realizes the degraded/fault/protection/takeover portion of the frozen M10.9.8 validation plan. The execution contract is `eng/m10983-degraded-fault-protection-takeover-matrix.json` and contains exactly eleven rows `DFP-01..DFP-11`:

1. invalid required supervisory measurement;
2. suspect/unavailable measurement operator truth;
3. protection active before a normal command;
4. protection trip during automated/assisted operation;
5. component fault;
6. instrumentation fault;
7. command rejected by a real permissive/interlock;
8. requested SupervisoryAutomatic degrading to effective Assisted;
9. operator manual takeover;
10. supported recovery after a declared degradation clears;
11. challenge active while degraded/protection truth is present.

## Validation-only composition

DFP-01/08/10/11 use a test-local exact-v4 scenario/pack. It reuses the authoritative `integrated-operations-desktop-stable@4` runtime, the existing M8.3 `instrumentation.sensor-unavailable` fault seam, and the existing bounded-demand challenge/scoring/evaluator contracts. The local fault makes the canonical `power` measured signal unavailable at logical step 2 and clears it at step 5. Nothing is registered into the production scenario/challenge catalog.

The evidence must show:

- requested `SupervisoryAutomatic` remains the operator request;
- effective authority becomes `Assisted` with `Degraded` health while the required measurement is invalid;
- no true-state fallback is fabricated;
- MISSION publishes the same requested/effective/degradation truth and remains observational;
- external demand remains challenge-owned and available while the non-protection measurement degradation is active;
- after the declared fault clears, recovery occurs only through the canonical M5 supervisory coordinator and the valid measured frame;
- canonical protection reset remains separately owned by `ProtectionSystemSolverTests`: reset is accepted only after the safe-threshold and permissive conditions are satisfied.

## Protection precedence

A separate exact-v4 production-bound challenge row requests `SupervisoryAutomatic`, then invokes the canonical reactor SCRAM action. A later normal rod-withdraw command cannot clear or bypass protection. The expected authority state is requested `SupervisoryAutomatic`, effective `Assisted`, health `SuspendedByProtection`; the bounded-demand challenge may transition to `Failed` because unexpected trip is an authored challenge failure condition, but neither challenge nor assistance owns the trip.

## Fault and permissive owners

M8.2 hydraulic and M8.3 instrumentation tests are rerun directly. The M4.5 generator close-check owner is rerun at Simulation level to prove an unsynchronized breaker close is rejected by the real canonical permissive and leaves no electrical-load torque. M10.9.5.4 observed-response evidence is rerun so a rejection remains feedback rather than a fictional plant-effect delta.

## Replay ownership boundary

M10.9.8.3 deliberately does **not** claim replay/checkpoint equivalence for every degraded row. That matrix is M10.9.8.4 ownership. This milestone freezes deterministic, owner-correct degraded state and authority transitions that M10.9.8.4 must replay.

## Validation

Run:

```bat
dotnet build
dotnet test
scripts\run-m10983-degraded-fault-protection-takeover-audit.cmd
```

Promotion requires both artifact markers:

- `m10983-integration-composition-passes=True`
- `m10983-degraded-fault-protection-takeover-passes=True`

No separate manual HMI gate is introduced here; the end-to-end degraded/fault/protection HMI acceptance remains M10.9.8.5 ownership.
