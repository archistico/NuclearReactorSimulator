# M10.9.4.1-H.29 — Production Activation Candidate

## Status

**CANDIDATE.** Built directly on the user-validated H.24 Requalification 1 post-H.28 baseline. H.29 does not authorize production-default activation; H.30 owns the final Phase H policy decision.

## Purpose

H.19–H.28 plus the post-H.28 H.24 requalification have already qualified the corrected four-node path numerically, operationally, under protection/transient stress, off-design, for long duration and for bounded runtime cost. H.29 therefore does not add another solver or retune the plant. It creates a separately reviewable production-default candidate around the already-qualified chain:

```text
P060/F040
  -> four-node branch continuity
  -> H.9 corrected candidate
  -> H.20 eligibility / rollback
  -> H.22 corrected commit seam
```

The H.28 classification remains **bounded-but-costly** and must be carried unchanged into H.30.

## Deployment/versioning contract

The existing exact initial condition remains immutable:

```text
integrated-operations-desktop-stable v2
  policy: ExplicitCommittedState
  role: authoritative current default + operational rollback/reference
```

H.29 adds a separate exact-version candidate:

```text
integrated-operations-desktop-stable v3
  policy: FourNodeBranchContinuityCorrectedCommitOptIn
  role: H.29 production-default candidate only
```

A deployment-level selector resolves the requested policy before runtime construction. An explicit kill request always wins and resolves back to v2. H.29 never changes the meaning of v2 and does not reinterpret a running session from one policy to another.

Inside the v3 corrected runtime, the already-validated H.20 authority remains fail-closed: a denied or unsafe corrected candidate stays explicit for that same fixed step.

## Production telemetry contract

H.29 adds an observational counter over telemetry already emitted by H.20/H.22. It records:

- observed/four-node telemetry steps;
- P060/F040 triggered steps;
- H.20 candidate-eligible steps;
- H.22 commit-authorized steps;
- corrected commits;
- explicit fallback steps;
- rollback count and typed rollback-reason counters;
- commit-reason counters;
- fallback-commit violations;
- unsafe-commit violations;
- untargeted branch disagreements.

This counter has **no state-commit, rollback, protection or control authority**. It is intentionally not projected into `ControlRoomSnapshot`; operator-facing HMI remains separate from internal numerical diagnostics.

## Save/replay/checkpoint compatibility

H.29 registers both exact versions for qualification:

- standard integrated-operations scenario remains pinned to v2 explicit;
- a separate H.29 scenario is pinned to v3 corrected;
- recording stores the exact v3 initial-condition reference;
- full replay and checkpoint/seek must reproduce the recorded candidate trace exactly;
- the desktop application registry contains both v2 and v3 so candidate archives are loadable through the normal replay path;
- unknown future versions fail closed rather than aliasing v2 or v3.

## Frozen numerical/runtime contract

H.29 does not change:

- the 10 ms production fixed step;
- P060/F040 trigger limits;
- H.9 finite-difference Jacobian / damped Newton mathematics;
- H.20 eligibility and typed rollback rules;
- H.22 corrected-state ownership;
- four-node target set `steam|stop-out|header|turbine-inlet`;
- 2% pressure / 5 K bounded previous-phase continuity;
- physical coefficients;
- the existing standard v2 scenario identity;
- H.28 performance ceilings or its `bounded-but-costly` classification.

## Focused qualification

The H.29 explicit audit is intentionally bounded. It does not rerun H.24 or H.28. Frozen evidence fingerprints establish those prerequisites, then the audit verifies:

1. authoritative policy resolution remains v2 explicit;
2. v3 resolves to the corrected candidate;
3. explicit kill of a v3 request resolves to v2 explicit;
4. a bounded v3 manoeuvre produces real H.20/H.22 trigger/eligibility/authorization/commit telemetry with no unsafe/fallback commit;
5. internal diagnostic counters match runtime telemetry and remain absent from the operator snapshot;
6. deterministic candidate runs repeat exactly;
7. exact-version v3 recording, full replay, checkpoint and seek reproduce exactly;
8. existing v2 remains independently resolvable and unchanged.

Required pass flags:

```text
four-node-production-activation-candidate-passes=True
h29-audit-passes=True
h30-closure-review-unblocked=True
```

## Boundary after a green result

A green H.29 result means the corrected path is a technically qualified **production activation candidate**. It still does not switch the authoritative default. H.30 must evaluate the complete H.19–H.29 evidence chain and choose one of the roadmap-defined outcomes:

- `ACTIVATE`;
- `OPT-IN ONLY`;
- `REMAIN EXPLICIT`.

Until that H.30 decision, v2 `ExplicitCommittedState` remains authoritative.
