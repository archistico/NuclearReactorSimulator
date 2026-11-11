# M10.9.8.1 REV1 — Manual validation-matrix acceptance checklist

**ACCEPTED 2026-08-22.** Automated gate was green and the user explicitly directed the project to proceed to M10.9.8.2; the frozen v1 matrix is therefore the validated execution contract.

This is a **contract review**, not an HMI execution checklist. REV1 keeps `src/` and `tests/` unchanged from M10.9.7.5 Hotfix 1 VALIDATED. Complete it only after build, the complete ordinary suite and `scripts\run-m10981-integrated-validation-matrix-audit.cmd` are green.

- [ ] Confirm M10.9.7.5 Hotfix 1 is the only validated baseline and M10.9.7 is CLOSED.
- [ ] Confirm REV1 contains no compiled/runtime or test-surface M10.9.8.1 changes; the matrix validator is external to `src/` and `tests/`.
- [ ] Confirm Docs1 updates only documentation/external validation: the user manual reflects M10.9.7 CLOSED, documents `MISSION`/demand-request-output/requested-effective authority, and does not claim M10.9.8.1 adds runtime functionality.
- [ ] Confirm the healthy matrix is exactly 3 × 3: `Hidden|ChecklistOnly|Guided` × `Manual|Assisted|SupervisoryAutomatic`.
- [ ] Confirm all nine HAA rows use the same exact challenge/profile and representative command schedule.
- [ ] Confirm `SupervisoryAutomatic` healthy rows require a configured objective and healthy required measurements; no automatic success is assumed without those preconditions.
- [ ] Confirm the 11 required scenario families are represented: healthy load, synchronization/loading, blocked permissive/interlock, degraded supervisory measurement, protection trip, equipment fault, instrumentation fault, manual takeover, challenge/demand-following, checkpoint/replay and terminal mission with continuing plant time.
- [ ] Confirm INT-12 is explicitly a validation-only composition and does **not** authorize production scenario/fault registration.
- [ ] Confirm every row records exact scenario/composition identity, exact profile version, assistance, requested/effective authority, commands/actions, protection/fault expectation, replay/checkpoint requirement, operator-visible evidence, manual observations and failure owner.
- [ ] Confirm assistance and plant-control authority are independent axes.
- [ ] Confirm protection is always superior to assistance, challenge, score and supervisory automation.
- [ ] Confirm demand/request/actual separation and expected/observed command-evidence separation remain mandatory.
- [ ] Confirm unavailable/suspect measured values cannot silently fall back to true state.
- [ ] Confirm MISSION remains presentation-only and F1–F8/no-F9 behavior is not reopened by this milestone.
- [ ] Confirm replay/checkpoint equivalence must be derived from canonical/versioned owners; opaque workstation state dumps remain forbidden.
- [ ] Confirm failed rows are routed to existing owners before any code change; Phase H/I numerical work is not reopened without direct contradictory evidence.
- [ ] Confirm no selected matrix-v1 row currently needs an RNG seed; `deterministicSeed = null` is intentional.

Acceptance phrase:

`M10.9.8.1 validation matrix accepted`

After that acceptance, M10.9.8.1 may be promoted and M10.9.8.2 may execute the frozen nine-row healthy assistance × authority matrix.
