# M10.9.4.1-H.13 — Thermodynamic Branch Continuity / Hysteresis Shadow Experiment

## Baseline

H.13 is built only on the user-validated H.12 inverse-branch-selection audit. H.12 confirmed the same mechanism at the two persistent H.9 failures:

- `steam`, interval 200, nominal `SaturatedMixture`;
- `stop-out`, interval 360, nominal `SuperheatedVapor`;
- saturated and superheated inverse roots overlap;
- coarse saturated detection toggles under tiny mass/energy perturbations;
- an earlier coarse-superheated result can shadow a still-valid later boundary-aware saturated root;
- `previousState` does not currently affect production branch selection.

Production remains `ExplicitCommittedState` at 10 ms.

## Question

Can a narrow continuity rule at only the two H.12 nodes remove the artificial inverse-map branch jump sufficiently for the unchanged H.9 Jacobian hydraulic corrector to converge on all seven frozen P060/F040 events?

H.13 does not change `SimplifiedWaterSteamThermodynamicModel.Resolve()`. It wraps the production resolver only inside the shadow experiment.

## Policies

### Production control

The unchanged resolver result. This must reproduce H.9: 5/7 converged and two line-search exhaustions.

### Previous-phase continuity

When both `SaturatedMixture` and `SuperheatedVapor` roots are available and the committed `previousState` phase has a valid root, select that root. Otherwise delegate to production.

The H.9 integrator reconstructs every trial from the committed interval state, so `previousState` is the committed physical phase and not mutable Newton-iteration history.

### Bounded previous-phase hysteresis

Use the same previous-phase root only while it remains close to the committed state:

- relative pressure drift <= 0.02;
- absolute temperature drift <= 5 K.

If either limit is exceeded, release to production selection. This prevents H.13 from becoming an unconditional permanent phase lock.

## Scope restriction

The alternative selector is enabled only for node IDs:

- `steam`;
- `stop-out`.

Every other node uses the production resolver unchanged.

## Audit evidence

For both policies H.13 records:

- convergence and line-search exhaustion over the frozen seven trigger events;
- pressure and flow fixed-point residuals;
- deterministic hydraulic evaluation work ratio;
- number of branch overrides versus production;
- number of previous-phase holds;
- hysteresis releases;
- phase transitions in the targeted decision trace;
- maximum avoided production pressure/temperature branch jump;
- hydraulic mass closure and energy ownership residuals;
- exact deterministic repeat.

## Qualification

A branch policy qualifies only if all seven frozen events converge under the unchanged H.9 tolerances, no line search exhausts, merit decreases on accepted H.9 steps, deterministic work remains within the H.9 limit, target branch chatter is absent and conservation/ownership remain within existing tolerances.

The focused H.13 audit itself may still pass if no policy qualifies. That outcome means branch continuity alone is insufficient and must not be hidden by looser solver tolerances or broader phase locking.

## Production isolation

H.13 does not:

- modify the body or ordering of production `Resolve()`;
- modify H.3-H.9 hydraulic solvers;
- change `PlantNetworkOrchestrator` routing;
- retune P060/F040;
- alter physical coefficients;
- change the 10 ms production timestep;
- commit any shadow candidate;
- introduce production hysteresis, active-set logic or a semi-smooth solver.

## Decision after H.13

If bounded hysteresis qualifies, prefer it over unconditional continuity for broader shadow qualification because it provides an explicit release condition. If only continuity qualifies, qualify that narrower hypothesis further before designing a release rule. If neither qualifies, inspect the H.13 traces and move to an explicit thermodynamic active-set/semi-smooth formulation rather than adding more global hydraulic solver complexity.
