# Pre-M11 Engineering Review Consolidation

## Status

**PLANNING CONSOLIDATION — not M10 promotion evidence.**

This document is the index for the engineering decisions made from the source-driven reviews performed during final M10 validation and the P1A/P2R planning window. It does not change production physics, runtime semantics, archive identities, test acceptance thresholds or the frozen M10 long-validation workload.

The current M10 long campaign remains authoritative for M10 closure. At the time this documentation consolidation was prepared, the first long acceptance execution was still collecting evidence and LR-H1 had already produced a real healthy exact-v4 water/steam envelope failure; the failure must be diagnosed from the complete artifact set before any production correction is selected.

## Review streams

### A. Nuclear-code V&V

Primary document: [`PRE_M11_NUCLEAR_CODE_VV_REVIEW.md`](PRE_M11_NUCLEAR_CODE_VV_REVIEW.md).

Retained outcome:

- phenomenon-scoped qualification language;
- 27-row final M10 V&V matrix;
- curated current-owner cumulative gate;
- separate long gate;
- frozen acceptance criteria before execution;
- explicit distinction among verification, model evidence, integral qualification and HMI acceptance;
- non-regression/change-impact policy for later milestones.

### B. Digital I&C / human-system safety

Primary document: [`PRE_M11_DIGITAL_IC_HUMAN_SYSTEM_SAFETY_REVIEW.md`](PRE_M11_DIGITAL_IC_HUMAN_SYSTEM_SAFETY_REVIEW.md).

Retained outcome:

- architecture invariants and protection/control separation;
- human-automation function allocation;
- deterministic Digital I&C hazard catalog;
- HMI classic failure-mode checklist;
- proportional runtime/dependency assurance;
- M11 remains feature-frozen and absorbs only release-assurance consequences;
- new post-release feature work is owned mainly by M13.9 rather than leaking into M11.

### C. Operating-point equilibrium and stability

Primary document: [`REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md`](REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md).

Retained outcome:

- steady state is not defined by “looks quiet for 300 s” alone;
- observational residual census precedes any solver or physics change;
- closed-loop and fixed-input/plant-hold diagnoses remain distinct;
- residuals are vector-valued and source-owned;
- thermodynamic domain headroom is diagnostic only and may never clamp/correct runtime state;
- a future operating-point trimmer may modify only allow-listed initial/control-memory variables, never model coefficients or V&V thresholds;
- any repaired production seed requires a new exact identity rather than reinterpretation of `integrated-operations-desktop-stable@4`;
- formal implementation home is M12.0, while a minimal LR-H1 residual census may be used earlier only if required to diagnose the current M10 blocker.

### D. Plant dynamics, thermal-hydraulics and reactor physics

Primary document: [`PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md`](PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md).

Retained outcome from the five-source Review 1:

- sustained load changes are treated as whole-unit energy-balance/control problems rather than an isolated generator-MW rewrite;
- fast grid/governor response, stored-energy response, heat-source response and final operating-point settling are separate capabilities and time scales;
- steam/feedwater/inventory and hydraulic head/loss balance remain part of operating-point qualification;
- ideal phase separation is explicitly limited relative to carryover/carry-under/separator behavior;
- global point kinetics remains a reduced model distinct from full-core space-time kinetics;
- P1A remained unchanged during the literature review and has now returned execution PASS / overall `INCONCLUSIVE`; the literature informs P2R1/Plan Amendment 2, mandatory Deep Review Pass 2 and later P2R2/P3/M12/M14/M15 work only.

## Consolidated implementation map

| Area | Decision | Home |
| --- | --- | --- |
| M10 closure | Preserve frozen V&V/long criteria; diagnose failures without post-hoc tolerance widening. | Current M10 final gate |
| Release assurance | Freeze architecture/function allocation, dependency identity, compatibility, timing/memory/package evidence and representative operator acceptance. | M11 |
| Operating-point equilibrium | Residual taxonomy → closed-loop inspector → headroom/trends → mass/energy/circulation balance → plant-hold seam → bounded trimmer if necessary → stability qualification. | M12.0 |
| Extreme-operations physics | Directionality, near-zero conditioning, pump-energy ownership, post-trip heat removal, integrity and incident-state foundations. | M12.1+ |
| Digital I&C degradation / HMI | stale/delayed/lost/inconsistent evidence, command-feedback delay, automation transparency and anti-keyhole context. | M13.9 |
| Integrated UX closure | Final M13 integrated experience after Digital I&C slice. | M13.10 |
| Spatial fidelity | Explicit solved/derived/mapped/interpolated local evidence; global point kinetics remains distinct from space-time/full-core kinetics. | M14 |
| Accident consequence models | Causal owner-driven consequence families only after M12/M14 prerequisites. | M15 |

## Explicit non-decisions

The reviews do not authorize:

- widening M10 long-run tolerances after seeing a failure;
- changing exact historical identities in place;
- hidden equilibrium correction during gameplay;
- PWR/BWR/RBMK textbook constants copied directly into the reference plant without dedicated evidence;
- a hard-real-time or safety-grade claim;
- a network/fieldbus simulator inside M11;
- duplicate protection algorithms presented as independent diversity;
- UI-owned physics, protection or plant authority;
- quantified software failure probabilities without defensible data/model scope.

## Source provenance

See [`research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md`](research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md) for exact bibliographic provenance and retained/non-retained source consequences.


## Deep Engineering Section Review 2

The five-source plant-dynamics/thermal-hydraulics/reactor-physics stream has completed a detailed second pass over the exact sections previously marked for deeper study. The review is indexed by [`research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md`](research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md) and its traceability matrix. It refines, rather than replaces, Review 1 and keeps P1A/P2R execution governance unchanged.

The main additional planning consequences are: explicit requested/effective/physical load-control layers; separator carry-under/pressure-loss feedback as a stated idealization gap; headroom versus sustainable flexibility; thermal storage as dynamic filtering; spatial/state-dependent rod-worth as future M14 fidelity; and declared preservation targets for any future coarse neutronic homogenization.


## Addendum — Todreas/Kazimi Volumes I–II

The pre-M11 research baseline now includes two additional thermal-hydraulic references. Their pre-Plan-Amendment-2 deep review strengthens three existing project directions without authorizing runtime change:

1. operating-point evidence must include conserved inventory and canonical hydraulic compatibility, not just apparent output settling;
2. the current water/steam model must be described explicitly as reduced HEM-like fidelity and must not be used to claim slip/nonequilibrium/density-wave physics it does not own;
3. future channel-group/scaling work must declare what is preserved and what distortion/fidelity limitation remains.

Plan Amendment 2 is now authored as a documentation/planning candidate. A second detailed pass is mandatory before P1B implementation; its disposition may approve the amendment as authored, require an amendment hotfix, or stop the plan.

## Todreas/Kazimi Deep Review Pass 2 — amendment audit

The second Todreas/Kazimi pass is intentionally different from Pass 1: it audits the **actual validated Plan Amendment 2** rather than re-reading the books generically. Its candidate disposition is `PASS-AS-AUTHORED`. The current runtime already exposes sufficient canonical conservation, inventory, hydraulic, control, turbine and generator ownership evidence to prepare P1B without adding new physical equations.

The review additionally freezes these interpretation boundaries: one-second samples are slow-tail downsampling rather than a fast-stability proof; project time windows are not prototype RBMK constants; the existing `SecondaryCycleHeatBalanceAudit`/network audits are reused instead of inventing a second energy ledger; no second hydraulic solve is allowed in diagnostics; no scalar cross-domain owner score is used in P1B v1; and absent two-fluid/ONB/NVG/density-wave physics remains explicitly unavailable.

