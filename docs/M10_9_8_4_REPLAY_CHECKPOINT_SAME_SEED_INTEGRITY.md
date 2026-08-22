# M10.9.8.4 — Replay / Checkpoint / Same-Seed Integrity

## Scope

M10.9.8.4 is an automated integration/evidence milestone stacked on M10.9.8.3 VALIDATED. It does not add production runtime behavior, Simulation physics, new fault/protection/scoring ownership, archive schema fields, fingerprint algorithms or plant-command authority.

Its purpose is to prove that representative M10 integrated states reconstruct identically through the canonical M9.1/M10.7 recorder/replay/checkpoint seams and through the M10.9.6.5 challenge replay projector.


## Hotfix 1 — protection/authority observation boundary

The original M10.9.8.4 candidate was not validated: the protection row checked `SuspendedByProtection` immediately after the step that committed the reactor SCRAM. The canonical authority owner observes committed protection on the following deterministic tick. Hotfix 1 therefore captures the protection checkpoint only after that next tick, matching the already validated M5 authority integration and M10.9.8.3 protection-precedence contracts. No production runtime or protection semantics are changed.

## Same-seed meaning

The simulator has no runtime pseudo-random state that needs a new serialized seed. For this milestone, same-seed means:

- the same exact versioned scenario and initial-condition identity;
- the same accepted operator-action trace;
- the same accepted M5 automation-intent trace;
- fresh independently loaded sessions.

Those inputs must produce the same ordered recorder-frame fingerprints, events, operator actions, automation intents, checkpoints and final challenge replay fingerprint.

No RNG field, opaque physical state blob or opaque challenge checkpoint state is introduced.

## Integrity rows

`eng/m10984-replay-checkpoint-same-seed-integrity-matrix.json` freezes four representative state classes:

- **RCI-01** — healthy bounded-demand SupervisoryAutomatic operation;
- **RCI-02** — required measurement unavailable, requested SupervisoryAutomatic degraded to effective Assisted, followed by deterministic recovery;
- **RCI-03** — canonical reactor SCRAM with SupervisoryAutomatic suspended by protection;
- **RCI-04** — manual takeover from healthy SupervisoryAutomatic operation with stale supervisory objective cleared.

For every row the gate requires:

1. fresh same-seed repeat equivalence;
2. full replay equivalence through `ScenarioFullReplayRunner`;
3. replay-backed checkpoint prefix restoration followed by the identical live continuation;
4. equivalent M10.9.6.5 challenge replay projection fingerprint.

## Ownership preserved

- every-step plant replay fingerprint: M9.1/M10.7 recorder/replay owner;
- archive/checkpoint schema: M10.7 schema v1;
- authority/objective intent replay: M5 session seam plus M10.7 recorder;
- lifecycle/demand/score reconstruction: M10.9.6.5;
- degraded/fault/protection/takeover semantics: M10.9.8.3 owners;
- operator-visible manual acceptance: M10.9.8.5.

M10.9.8.4 does not reinterpret any of these contracts.

## Validation

Run:

```bat
dotnet build
dotnet test
scripts\run-m10984-replay-checkpoint-same-seed-integrity-audit.cmd
```

Promotion requires:

```text
m10984-replay-checkpoint-same-seed-integrity-passes=True
```

There is no separate manual gate for M10.9.8.4. Manual integrated HMI/keyboard/session acceptance remains M10.9.8.5.
