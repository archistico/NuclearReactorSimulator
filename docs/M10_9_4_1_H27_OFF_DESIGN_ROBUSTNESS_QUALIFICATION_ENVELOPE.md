# M10.9.4.1-H.27 — Off-Design Robustness & Qualification Envelope

## Status

**VALIDATED (Hotfix 1)** on 2026-08-19. Build, complete ordinary tests and the focused H.27 gate passed.

Validated result: 6 scenarios / 2,080 runtime steps / 529 triggers and corrected commits; 0 rollback, fallback-commit, unsafe-commit or untargeted-disagreement violations; four scenarios classified `corrected-qualified` and two `protected-boundary`.

## Purpose

H.24 qualified duration and the four nominal H.19 profiles. H.25 qualified representative protection/transient interactions. H.26 proved atomic same-step explicit fallback for all eight typed H.20 rollback reasons plus denial controls.

H.27 asks the next question: **where does corrected ownership remain qualified outside the H.19 nominal profile amplitudes, and where should the system deliberately remain fail-closed?**

This milestone does not try to maximize the envelope and does not treat rollback or a canonical protection trip as automatic failure. The objective is a bounded, evidence-backed map.

## Numerical runtime

H.27 does not retune or replace:

- P060/F040;
- H.9;
- bounded 2% pressure / 5 K hysteresis;
- target set `steam|stop-out|header|turbine-inlet`;
- H.20 authority/rollback semantics;
- H.22 corrected-commit seam;
- physical coefficients;
- the 10 ms fixed step.

Standard current-v2 remains `ExplicitCommittedState`. The corrected path remains separately opt-in through `FourNodeBranchContinuityCorrectedCommitOptIn`.

## Staged off-design matrix

H.27 deliberately stays inside defensible educational-model inputs while moving beyond the H.19 nominal amplitudes:

1. **10 MWe request** from the 5 MWe desktop point;
   - qualification evidence requires that the 10 MWe requested-load point is actually observed; a subsequent canonical protection action is mapped as `protected-boundary`, not treated as an automatic H.27 failure;
2. **50% condenser-cooling capacity**;
3. **25% condenser-cooling capacity**;
4. **10 MWe + 50% cooling capacity**;
5. **0 MWe + 25% cooling capacity**;
6. **total cooling loss** over a bounded observation window near the canonical condenser-backpressure protection boundary.

The matrix is targeted and short. It does not rerun the 4h31m55s H.24 long-horizon gate.

## Qualification classifications

Each scenario is classified from observed committed telemetry:

- `corrected-qualified` — corrected commits occur without protection boundary or fail-closed fallback;
- `safe-fallback-envelope` — H.20/H.22 declines corrected ownership and explicit remains authoritative safely;
- `protected-boundary` — canonical protection acts while numerical ownership remains safe;
- `observed-no-trigger` — the bounded window did not exercise P060/F040.

For the focused H.27 matrix, every scenario is required to exercise P060/F040 so `observed-no-trigger` is diagnostic only and fails the focused qualification sample.

## Fail-closed rules

Rollback is allowed. Protection action is allowed in protection-adjacent cases. The following are not allowed:

- corrected commit while H.20 is not eligible;
- corrected commit while rollback is required;
- unsafe corrected commit outside H.20 residual/conservation guards;
- partial/mixed ownership;
- conservation/ownership residual outside the H.22 limits;
- nondeterministic repeat;
- exposure of the H.26 audit hook through public production construction.

## Evidence products

The focused gate writes:

```text
artifacts/h27-four-node-off-design-qualification-envelope/
  00-progress.txt
  01-four-node-off-design-qualification-envelope.summary.txt
  02-off-design-step-telemetry.csv
  03-off-design-qualification-envelope.csv
  04-off-design-qualification-metrics.csv
```

The per-scenario envelope CSV is the principal H.27 result. It records classification, trigger/commit/rollback counts, trip observations, requested-load range and maximum condenser pressure.

## H.26 provenance

H.27 freezes the user-reported validated H.26 focused summary. H.26 established:

```text
challenges                         12
rollback challenges                8
nonrollback denial controls        4
same-step explicit fallback     12/12
corrected commits                  0
partial commit violations          0
deterministic repeat            True
```

The public orchestrator decision transform remains inactive.

## Decision

A green H.27 documents a bounded qualification envelope. It does **not** imply that all model-valid states are corrected-qualified and it does not authorize production-default activation.

H.28 was attempted next and failed only its performance ceilings as `unbounded-regression`; numerical safety/determinism remained green. Current continuation is **H.28.1-A — Corrected-Path Performance Attribution**.
