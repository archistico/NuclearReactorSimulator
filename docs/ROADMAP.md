# Roadmap

This file lists remaining work. Detailed completed chronology is under `history/`.

## Current checkpoint

Validated production decision: **H.30 Requalification 1 — ACTIVATE**.

Validated Phase-I reference: **I.3 Hotfix 2** — 300 s / 30,000-step exact-v3 production reference, seven frozen slopes, 19 regression budgets.

Current candidate: **I.4 — Known Limitations & Legacy Retirement Review**.

## Phase I — remaining closure work

### I.4 — Known limitations and legacy retirement review

Required:

- reconcile current limitations with the activated v3 policy and validated I.3 reference;
- make non-zero final-window inventory/energy drifts explicit rather than treating I.3 as asymptotic steady-state proof;
- verify H.5/H.21 modes are not production, exact-version or current-CI dependencies;
- enumerate remaining source/test seams;
- preserve exact-version scenario/save/replay identities;
- remove legacy source only if executable provenance no longer depends on it.

Candidate decision: defer physical deletion through M10.9.4.1 closure because historical executable seams still remain.

### I.5 — Cumulative M10.9.4.1 closure gate

Require:

- ordinary suite;
- H.30 RQ1 production-policy evidence;
- I.3 authoritative reference + frozen budgets;
- 60 s gameplay;
- protection/replay determinism;
- conservation/inventory slopes;
- reference-plant scale contract;
- H.28 performance classification;
- scheduled long gates;
- I.4 known-limitations/legacy review.

Only a green I.5 unblocks M10.9.5.

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
