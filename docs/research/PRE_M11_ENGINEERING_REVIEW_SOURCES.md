# Pre-M11 Engineering Review Sources

## Status

**RESEARCH / PLANNING PROVENANCE — not validation evidence and not a licensing claim.**

This file records the three book-length sources reviewed during the final M10 / pre-M11 planning window and the project decisions that were retained from each source. The source books themselves are not bundled in project candidate ZIPs.

## 1. Nuclear-code development, V&V and application

**Jun Wang, Xin Li, Chris Allison, Judy Hohorst (eds.), _Nuclear Power Plant Design and Analysis Codes: Development, Validation, and Application_, Woodhead Publishing / Elsevier, 2021.**

Retained principles:

- distinguish verification, model assessment/validation evidence, integral system qualification and user acceptance;
- qualify phenomena and bounded domains explicitly rather than making a blanket “validated code” claim;
- separate single/separate-effect evidence from integral/whole-system evidence;
- treat multiphysics coupling, timestep, convergence and numerical conditioning as qualification subjects;
- use tiered unit/integration/system/long validation and non-regression evidence;
- compare against previously accepted evidence without blindly rerunning obsolete historical experiments;
- state model limitations and qualified ranges explicitly.

Project consequences:

- `PRE_M11_NUCLEAR_CODE_VV_REVIEW.md`;
- 27-row `eng/m10-final-vv-matrix.json`;
- curated final cumulative M10 gate;
- separate long M10 validation gate;
- `CHANGE_IMPACT_REVALIDATION_POLICY.md` and release-evidence planning.

Not imported:

- plant-specific PWR/LWR constants;
- professional/licensing-grade claims;
- wholesale replacement of the simulator’s reduced-order physics with TRACE/ATHLET/SAM/CFD-style models.

## 2. Digital I&C, software safety and human-system integration

**Committee on Application of Digital Instrumentation and Control Systems to Nuclear Power Plant Operations and Safety, _Digital Instrumentation and Control Systems in Nuclear Power Plants: Safety and Reliability Issues_, National Academy Press, 1997.**

Retained principles:

- explicit system-level allocation of control, protection, supervision and operator functions;
- protection/control separation and deterministic manual takeover;
- timing as part of control correctness while preserving the explicit non-claim that the desktop simulator is hard real-time;
- common-mode software failure and the distinction between duplication, design diversity and functional diversity;
- software assurance as more than test execution alone;
- human factors including data overload, keyhole effect, mode errors, workload imbalance, clumsy/opaque automation and situation awareness;
- deterministic treatment of stale/delayed/lost/inconsistent information as a useful future educational direction;
- proportional COTS/dependency assurance rather than nuclear-grade dedication of .NET/Avalonia.

Project consequences:

- `PRE_M11_DIGITAL_IC_HUMAN_SYSTEM_SAFETY_REVIEW.md`;
- `DIGITAL_IC_ARCHITECTURE_INVARIANTS.md`;
- `HUMAN_AUTOMATION_FUNCTION_ALLOCATION.md`;
- `DIGITAL_IC_HAZARD_CATALOG.md`;
- `HMI_CLASSIC_FAILURE_MODES_CHECKLIST.md`;
- M11 release-assurance work;
- M13.9 Digital I&C Degradation & Automation Transparency;
- deferred protection-diversity inventory before any independence/diversity claim.

Not imported:

- 1997 regulatory positions as current requirements;
- a distributed hard-real-time I&C implementation;
- invented quantitative software failure probabilities;
- cosmetic duplicate protection algorithms presented as independent systems.

## 3. Reactor physics, heat removal and operating-point self-consistency

**John R. Lamarsh and Anthony J. Baratta, _Introduction to Nuclear Engineering_, 3rd ed., Prentice Hall, 2001.**

Retained immediate principle:

- in coupled reactor/thermal problems, an operating point is meaningful only when interdependent quantities are mutually self-consistent; an assumed state must reproduce a compatible state after the coupled calculation rather than merely appear quiet for a short observation interval.

Project consequence already planned:

- `REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md`;
- `M10_LR_H1_EQUILIBRIUM_DIAGNOSTIC_PLAN.md`;
- M12.0 Reference Operating-Point Equilibrium & Stability Qualification;
- observational residual inspector first, bounded trimmer only if evidence requires it;
- exact-version immutability if a repaired production operating-point seed is eventually required.

Additional candidate topics were identified but are **not yet authorized implementation work**; see `LAMARSH_FOLLOW_UP_CANDIDATES.md`.

Not imported:

- reactor-type-specific numerical values or correlations merely because they appear in the textbook;
- PWR/BWR thermal limits as direct RBMK-like simulator limits;
- historical RBMK-specific behavior into the default reference reactor without an explicitly versioned historical model.

## Governing use rule

These sources are engineering inputs. Project code changes still require an explicit owner, scope, acceptance criterion, revalidation impact and compatibility decision. A book-derived idea is never sufficient reason by itself to change a physical coefficient, safety threshold, archive identity or V&V tolerance.
