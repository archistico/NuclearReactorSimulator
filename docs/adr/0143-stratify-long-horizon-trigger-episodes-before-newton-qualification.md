# ADR 0143 — Stratify long-horizon trigger episodes before Newton qualification

## Status

Accepted for M10.9.4.1-H.17 Hotfix 4 candidate.

## Context

H.17 Hotfix 3 completed the 30,000-interval explicit reference trajectories and exhaustive P060/F040 census, discovering 3,046 above-threshold intervals: 837 steady-long, 1,014 load-pulse, 175 cooling-pulse and 1,020 combined-load-cooling. The reference trajectories completed normally; the cost came from treating every timestep inside prolonged above-threshold periods as an independent H.9/Newton qualification event.

P060/F040 remains a valid stiffness detector. Retuning it merely to reduce audit cost would change the numerical policy under investigation and is rejected.

## Decision

Keep exhaustive P060/F040 discovery across all 30,000 intervals, but separate trigger **census** from nonlinear **qualification**. Deterministically coalesce nearby trigger intervals into episodes using a maximum quiet gap of 25 intervals. Every episode must contribute first, last and hardest representatives. Preserve every H.16 control trigger, represent profile action boundaries and add temporally distributed representatives per profile, subject to a hard maximum of 512 H.9 qualification events.

H.9/Newton and all-node candidate inverse-branch scanning operate on the stratified representatives. All census triggers continue to force committed target-selection observations. Determinism uses distributed sentinels. Mandatory representatives are never silently dropped; exceeding the bounded budget fails the gate.

## Consequences

The 30,000-interval operating-domain census remains exhaustive while the expensive nonlinear work is bounded and interpretable. A green H.17 Hotfix 4 result qualifies the episode-stratified evidence set, not every individual above-threshold timestep. Production remains explicit and activation remains a later, reversible decision with retained shadow evidence.
