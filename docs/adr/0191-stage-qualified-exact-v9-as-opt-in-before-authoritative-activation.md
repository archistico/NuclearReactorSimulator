# ADR 0191 — Stage qualified exact-v9 as an opt-in production policy before authoritative activation

## Status

Accepted / M10 Final exact-v9 production activation candidate design.

## Context

Diagnostic 11 Hotfix 2 qualifies `integrated-operations-desktop-stable@9` for 600 simulated seconds with effectively stationary mass, pressure, governor and energy behavior around 5 MWe. The remaining risk is no longer the operating point itself but deployment wiring: policy selection, exact-version registry availability, scenario identity, fail-closed rollback and deterministic equivalence.

The project already has a precedent for separating activation readiness from authoritative default switching: H.29 staged exact-v3 before H.30 made a production decision, and I.5 staged exact-v4 before its final activation/closure gate.

## Decision

Add `DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate` as an explicit opt-in policy resolving exact-v9. Register exact-v9 in the desktop composition root and expose a distinct activation-candidate scenario identity.

Do **not** change `DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy` in this milestone. Exact-v4 remains authoritative and exact-v2 remains the explicit fail-closed kill/rollback reference.

The activation-candidate gate must rerun ordinary tests, current exact-v4 evidence, exact-v9 600 s qualification and a focused policy-path audit. The focused audit requires the selector-created engine to match direct exact-v9 deterministic fingerprinting while preserving conservation, moisture-drain ownership and healthy ~5 MWe operation.

## Consequences

A green activation-candidate result authorizes a separate authoritative activation decision. That later decision may switch the default to exact-v9 and rebind production scenario/mission identities, but must preserve exact historical versions. The replacement long is not authorized until authoritative exact-v9 activation itself is validated.
