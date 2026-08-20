# I.4 — Known Limitations & Legacy Retirement Review

## Purpose

I.4 follows the validated I.3 authoritative production reference. It does not change the H.30 RQ1 `ACTIVATE` decision or runtime numerics.

The review has two goals:

1. expose important non-zero I.3 final-window drifts as current known limitations;
2. determine whether the historical H.5/H.21 numerical modes can be physically deleted without losing current compatibility or required executable provenance.

## I.3 observations carried forward

The validated 300 s v3 reference has zero generation-health and targeted reverse-flow violations, but its final 60 s include:

- drum inventory slope: +8.2451672984622224 kg/s;
- main-steam-header inventory slope: -0.35293086123580603 kg/s;
- total-fluid internal-energy slope: -2.061802762164879 MW.

These are frozen regression observations, not calibration targets and not proof of asymptotic steady state.

## Legacy-mode review

`DeterministicHybridSemiImplicit` and `FourNodeBranchContinuityShadowIntegrated`:

- are not production-selectable;
- are not required by any exact-version production/save/replay identity;
- are not current-CI dependencies;
- each remains referenced by four source files and four test files.

I.4 therefore proposes `DEFER-SOURCE-REMOVAL`. Historical execution remains possible, but the modes must not re-enter production selection.

## Closure

A green I.4 unblocks I.5. It does not itself close M10.9.4.1.
