# ADR 0132 — Revise the shadow hydraulic corrector around a fixed-point residual and deterministic backtracking

## Status

Accepted for M10.9.4.1-H.7 candidate

## Context

M10.9.4.1-H.6 was validated as a negative numerical qualification. Over the exact 500 committed explicit current-v2 intervals used by H.5 Hotfix 2, the frozen `P060-F040` trigger selected 7 intervals. The H.4 primary `R015-I072` corrector converged on 5/7. A bounded six-profile Picard rescue sweep selected `R0125-I096`, but it converged on only 6/7 and therefore did not qualify. The validated H.6 summary reported a maximum selected-profile pressure residual of approximately 0.292 and flow residual of approximately 61.7 kg/s.

The H.3/H.6 Picard solver declares convergence by measuring the movement between consecutive relaxed iterates. That metric is useful for the historical prototype, but it is not a direct test of the nonlinear fixed-point equation. A smaller relaxation factor can make consecutive iterates move less even when the iterate remains far from the unrelaxed hydraulic map.

Production current-v2 has already been restored to the validated explicit 10 ms path by H.5 Hotfix 2. H.7 therefore has no need to alter production routing in order to revise the algorithm.

## Decision

H.7 introduces a new isolated `ResidualBacktrackingHydraulicCorrectorSolver`. It does not replace `SemiImplicitHydraulicPrototypeSolver` and is not wired into `PlantNetworkOrchestrator`.

For an accepted hydraulic-balance iterate `b`, H.7:

1. integrates the end-of-step state from the original committed inventories using `b` plus the frozen non-hydraulic balances;
2. evaluates the existing pipe/valve/pump laws on that iterate;
3. applies that **unrelaxed** hydraulic evaluation as the fixed-point map to obtain the mapped end state;
4. measures a relative pressure fixed-point residual between the iterate and mapped state;
5. measures an absolute flow fixed-point residual directly between the currently applied pipe/valve/pump flow iterate and the unrelaxed hydraulic evaluation returned by the map;
6. normalizes the two residuals by their fixed tolerances and defines the scalar merit as their maximum.

Convergence requires both the pressure and flow fixed-point residuals to satisfy their tolerances. It is not inferred from small relaxed-iterate motion.

When the current iterate is not converged, the solver attempts a deterministic line search from the current accepted hydraulic balances toward the unrelaxed hydraulic map. The H.7 audit profile starts at relaxation 1.0 and repeatedly multiplies by 0.5 down to 1/1024. A trial is accepted only when the normalized fixed-point merit residual strictly decreases. Invalid trial states are rejected and backtracking continues; rejected trials are never authoritative.

Each accepted candidate is still integrated exactly once from the original committed inventory using the accepted hydraulic balances. The accepted iterate carries component flows and pump hydraulic power through the same deterministic blend, allowing mass-rate closure and energy ownership to be checked on the actually applied iterate rather than only on the next unrelaxed map evaluation. The line search changes only the nonlinear numerical iterate, not conservation ownership or physical coefficients.

## Consequences

- The historical H.3/H.6 Picard solver remains available unchanged as baseline evidence.
- H.4/H.5 production-gate behavior remains unchanged.
- `PlantNetworkOrchestrator` remains on the explicit current-v2 production route established by H.5 Hotfix 2.
- H.7 can distinguish true fixed-point convergence from apparent convergence caused only by small damping.
- Backtracking behavior is deterministic and auditable through accepted relaxation factors, trial counts and residual traces.
- Hydraulic evaluation work is counted deterministically; wall-clock time is not used for numerical branching.
- A positive H.7 audit result authorizes only broader free-running/scenario shadow qualification. It does not authorize production activation.
- A negative H.7 audit result keeps production explicit and requires further nonlinear-solver work before broader qualification.

## Rejected alternatives

### Increase H.6 iteration limits again

Rejected. H.6 already demonstrated that a bounded relaxation/iteration sweep does not rescue every frozen trigger event. More of the same Picard iteration does not address the residual-definition weakness.

### Lower relaxation until iterate motion is small

Rejected. This is the precise failure mode H.7 is intended to avoid: small iterate motion is not sufficient evidence of proximity to the unrelaxed fixed point.

### Replace the production solver immediately

Rejected. H.5 Hotfix 1 already showed that direct activation before extended qualification is unsafe. H.7 is shadow-only.

### Retune P060/F040 or physical hydraulic coefficients

Rejected. The difficult intervals are fixed evidence. Avoiding them would invalidate the numerical investigation, and physical coefficients are outside the scope of a corrector-algorithm revision.
