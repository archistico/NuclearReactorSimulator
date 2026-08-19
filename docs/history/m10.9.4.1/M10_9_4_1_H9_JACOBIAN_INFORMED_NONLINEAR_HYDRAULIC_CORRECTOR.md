# M10.9.4.1-H.9 — Jacobian-Informed Nonlinear Hydraulic Corrector

## Purpose

H.9 is a shadow-only nonlinear-method revision built directly on the user-validated H.8 baseline. H.8 showed that safeguarded Anderson is correctly deterministic and conservative but still converges only 5/7 frozen P060/F040 events, with the same two line-search exhaustions seen in H.7. The best bounded Picard rescue remains H.6 at 6/7.

H.9 therefore changes method family: it uses local finite-difference derivative information for a damped Newton direction while retaining H.7's true unrelaxed pressure/flow convergence contract and monotone safeguard.

## Conservative coordinate system

`JacobianHydraulicCorrectorSolver` is separate from Picard, `ResidualBacktrackingHydraulicCorrectorSolver`, `AndersonHydraulicCorrectorSolver` and production routing.

A hydraulic iterate contains nodal hydraulic mass/energy balances, pipe/valve/pump flow rates and pump hydraulic fluid-work power. H.9 encodes this iterate in a square deterministic coordinate vector without breaking linear ownership invariants:

- one fluid node, chosen canonically by id, is the mass/energy anchor;
- mass-balance coordinates are stored for all non-anchor nodes; anchor mass flow is reconstructed as the negative sum;
- energy-balance coordinates are stored for all non-anchor nodes plus pump hydraulic power; anchor energy rate is reconstructed so total hydraulic node-energy rate equals pump hydraulic fluid-work power;
- all pipe, valve and pump flow rates are represented directly.

Every finite-difference probe, Newton target, damped trial and fallback trial is decoded through this coordinate system. Therefore H.9 never needs to violate hydraulic mass closure or energy ownership merely to estimate a derivative.

## Residual and Jacobian

The authoritative convergence evidence remains the H.7 unrelaxed fixed-point defect:

- maximum relative node-pressure defect <= `1e-5`;
- maximum absolute pipe/valve/pump flow defect <= `0.01 kg/s`.

For Newton construction H.9 also forms a coordinate residual `F(x)-x` using the same conservative coordinate layout. Coordinates are scaled deterministically by the larger current/mapped magnitude with physical floors, and the finite-difference Jacobian is built column by column with relative perturbation `1e-4`.

A forward probe is preferred. If it produces an inadmissible thermodynamic/inventory state, the matching backward probe is attempted deterministically. If neither side is usable, the Jacobian direction is rejected rather than silently modifying physics or tolerances.

The normalized linear system receives diagonal regularization `1e-8` and is solved by deterministic scaled partial pivoting. A pivot-ratio conditioning estimate above `1e12` rejects the Jacobian direction. The normalized Newton step infinity norm is capped at `8` before line search.

## Safeguarded damping and fallback

A Newton target is never committed directly. Deterministic backtracking starts at 1.0 and halves to 1/1024. A trial is accepted only when the unchanged H.7 normalized pressure/flow merit strictly decreases.

If the Jacobian cannot be built/solved, is rejected by conditioning safeguards, or its Newton direction cannot produce an accepted trial, H.9 falls back to the H.7 residual direction with the same monotone line search.

Every candidate state is integrated exactly once from the original committed inventory using the trial hydraulic balances plus the frozen non-hydraulic balances. Rejected probes and line-search trials are never authoritative state commits.

## Frozen audit contract

The H.9 focused audit replays exactly the same 500 committed explicit current-v2 intervals used by H.5-H.8 and must first reproduce:

- frozen trigger `P060/F040`;
- 7 triggered events;
- H.4 primary convergence 5/7;
- H.6 selected rescue convergence 6/7;
- H.7 residual/backtracking convergence 5/7 with two line-search exhaustions;
- H.8 safeguarded-Anderson convergence 5/7 with two line-search exhaustions.

The H.9 result then records convergence, pressure/flow merit, coordinate residual, Jacobian dimension, probe count, pivot-condition estimate, Newton-step magnitude, damping/backtracking, residual fallback use, deterministic hydraulic-evaluation work, exact repeat, inventory integration residual, hydraulic closure/ownership and gap versus the committed explicit trajectory.

`jacobian-informed-corrector-qualification-passes=False` is a valid audit result. It does not invalidate H.9 if build, ordinary tests and the focused audit pass. Instead it means the next work is diagnostic investigation of map switching/non-smoothness rather than arbitrary relaxation/tolerance changes.

## Production invariants

H.9 does not:

- activate hybrid production;
- replace Picard, H.7 or H.8;
- change `PlantNetworkOrchestrator` routing;
- retune P060/F040;
- change physical coefficients;
- add hidden flow filtering;
- change the 10 ms production timestep;
- commit any shadow candidate.

A positive H.9 result permits only broader free-running/scenario shadow qualification. Production activation remains a separate later decision gate.
