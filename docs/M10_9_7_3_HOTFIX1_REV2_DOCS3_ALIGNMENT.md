# M10.9.7.3 Hotfix 1 REV2 Docs3 — Desktop Host / Session Integrity Roadmap Alignment

## Scope

Documentation-only alignment over M10.9.7.3 Hotfix 1 REV2 Docs2. No runtime, test, validation-script, `eng/` or CI change belongs to Docs3.

## Current runtime validation state

- build: already green for Hotfix 1 REV2;
- complete ordinary suite: already green;
- `scripts/run-m10973-mission-performance-live-workspace-audit.cmd`: already green;
- `docs/M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md`: still pending;
- therefore Hotfix 1 REV2 is still CANDIDATE, not yet promoted.

## New accepted planning decisions

1. After REV2 manual validation/promotion, build **M10.9.7.3 Hotfix 2 — Desktop Host Failure & Session Save Integrity** exclusively on REV2 VALIDATED.
2. Hotfix 2 must contain expected numerical step failures at the desktop pump boundary without blanket exception swallowing.
3. Hotfix 2 must align start/reset/load/restore host failure policy.
4. Save must select destination before full export and use non-destructive temporary-write + safe replace/move semantics for supported local desktop storage.
5. Failure during write/replace must preserve the previous archive.
6. Remaining mixed App engineering-number formatting should align with the current invariant technical HMI convention.
7. M10.9.7.4 cannot begin until Hotfix 2 is validated.
8. M11.3 owns measured UI-thread/projection/notification/export responsiveness and any worker/off-thread ownership design.
9. M13 now owns stable canonical-ID selection/no silent command retargeting and staged `MainWindowViewModel` decomposition.

## Explicit non-scope

Docs3 authorizes no change to Simulation physics, fixed timestep, challenge/scoring/protection authority, archive schema, streaming API, worker-thread ownership or user-facing simulation speed.

## References

- `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md`
- `adr/0185-contain-desktop-runtime-failures-and-replace-session-archives-safely.md`
- `milestones/M10.9.7.md`
- `milestones/M11.md`
- `milestones/M13.md`
- `FORWARD_EXECUTION_PLAN_M10_9_7_TO_M15.md`
