# Roadmap

This roadmap lists **remaining work**. Detailed history of completed M10.9.4.1 A–I investigations is archived under `history/m10.9.4.1/` and summarized in milestone records.

## Current checkpoint

Authoritative fully validated baseline: **M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening**.

Validated evidence after I.2:

- I.3 Hotfix 4 Classifier Fix 1 — explicit-v2 targeted-train discontinuity classified and corrected-v3 suppression validated;
- I.3 Hotfix 5 — corrected-v3 300 s healthy reference requalification validated.

Current candidate: **H.30 Requalification 1 — Production Policy Re-review after I.3 Continuity Evidence**.

### H.30 Requalification 1 — current

Decision candidate: `ACTIVATE`.

Reason:

- H.28 cost remains bounded but materially higher than explicit;
- exact v2 now has a validated healthy-operation continuity defect: 338/338 generation-drop steps coincide with targeted stop/control/admission reverse flow in the 100 s comparison;
- exact v3 eliminates the defect in the same comparison and remains continuously healthy over 300 s / 30,000 steps;
- v3 telemetry remains fail-closed and deterministic;
- no numerical retuning is required.

If validated:

- exact v3 becomes authoritative desktop production default;
- exact v2 remains exact-version rollback/reference;
- original H.30 `OPT-IN ONLY` becomes superseded historical policy evidence.

## Phase I — remaining closure work

### I.3 — Authoritative reference trajectory and tolerance budgets

After H.30 RQ1 validation, rerun the I.3 reference baseline under the authoritative production policy.

Required outputs:

- healthy 300 s reference trajectory;
- per-step generation/continuity health;
- conservation/inventory observations;
- final-window slopes;
- deterministic fingerprint;
- versioned internal regression budgets derived from the validated authoritative trajectory.

Do not freeze budgets from the failed exact-v2 baseline.

### I.4 — Known-limitations and legacy retirement review

- reconcile `KNOWN_MODEL_LIMITATIONS.md` with the validated production policy;
- enumerate remaining H.5/H.21 source dependencies;
- remove legacy numerical modes only when executable provenance no longer depends on them;
- preserve exact-version scenario/save/replay identities.

### I.5 — Cumulative M10.9.4.1 closure gate

Before leaving numerical hardening, require a clean cumulative evidence set including:

- ordinary suite;
- current production-policy audit;
- 60 s gameplay journeys;
- healthy 300 s authoritative reference journey;
- protection evidence;
- replay/checkpoint determinism;
- conservation/inventory slopes;
- reference-plant scale contract;
- performance/cost classification;
- scheduled long gates as defined by the Phase-I CI contract.

Only a green cumulative Phase-I/M10.9.4.1 gate unblocks M10.9.5.

## M10.9.5 — Contextual Command Consequence Model

- focused commands explain direct effect, expected downstream influence, permissives/blockers and what to monitor;
- selected dependency chains can be highlighted on schematics;
- expected influence must remain distinct from observed response;
- no UI-side predictive reactor physics or invented causality.

## M10.9.6 — Operational Challenge & Energy-Demand Framework

- deterministic timed objectives for startup, shutdown, testing, power manoeuvring, stabilization and recovery;
- deterministic external electrical-demand profiles;
- multidimensional scoring with safety/procedure dominant;
- challenge timing based on logical simulation time, not wall-clock time.

## M10.9.7 — Mission & Performance Workstation

- current objective and progress;
- elapsed/target logical time;
- demand/output/error;
- score decomposition;
- assistance independent from plant-control authority.

## M10.9.8 — Integrated Human-Automation-HMI Validation Gate

- matrix validation across training assistance and plant-control authority;
- deterministic fault/invalid-measurement/protection/trip/manual-takeover cases;
- gauge/range, mimic/schematic, command-consequence and scoring integrity;
- degraded/fail-closed behaviour and protection priority;
- replay/checkpoint fidelity and same-seed determinism;
- manual HMI acceptance.

## M11 — Release hardening

After M10 closes:

- save/scenario/session migration hardening;
- performance/memory budgets across headless simulation, recorder/replay and desktop UI;
- packaging/publish pipeline;
- deployment verification;
- final documentation/manual cleanup.

## Deferred severe-incident direction

Persistent damage, fire, rupture/explosion and severe-accident progression remain approved future directions but must not interrupt the current M10 numerical/operational closure. They require explicit physical ownership, validated extreme-state numerics and deterministic replayable state before becoming authoritative gameplay systems.
