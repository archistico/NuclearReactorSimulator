# Model and validation decisions

These decisions summarize the enduring engineering constraints from M10. They do
not replace executable contracts or the current checkpoint in [PROJECT.md](../PROJECT.md).

## Preserve exact-version identities

Recorded scenarios, sessions and replay fingerprints retain their original meaning.
Canonical Exact-V9 is preserved independently of experiments with other closures
or operating-point seeds. A later model option cannot silently reinterpret an
existing exact version or activate itself as the default.

## Separate implementation, evidence and physical qualification

A successful implementation gate establishes the behaviour covered by that gate.
An evidence gate establishes that its observations and accounting are complete.
A physical qualification additionally requires its own acceptance conditions.
R1/R2 success and the completed H evidence gate therefore do not establish R3 PASS.

## Use independent references and explicit applicability

Production implementations may be compared with independent reference calculations;
they do not generate their own expected reference values. Reference equations,
parameter sets and tolerances are fixed before comparison. Generic solver accuracy,
plant-specific parameterization and whether a subsystem participates in the tested
operating point are separate questions.

## Keep thermodynamic options explicit

The reference-consistent C4 closure is an explicit opt-in mode. Its payload identity,
deterministic lookup, equivalence checks and allocation constraints are qualified
separately from historical modes. Changing a closure does not by itself establish
that the authored plant inventory is a stationary operating point.

## Diagnose ownership before changing physics

Conservation, storage rates, transport properties, hydraulic coupling and controller
responses must be traced to their owners. A failed integrated envelope does not
independently prove a controller defect. Numerical rejection before a plant call
does not demonstrate that the plant has no feasible state.

## Keep equilibrium investigations bounded

The H investigation fixes non-fluid initial conditions and varies only its authored
24 fluid mass/energy coordinates. Residual definitions, scales, coordinate bounds,
search accounting and confirmation conditions remain fixed. Its inconclusive result
does not authorize a wider box, altered gains, another trim family or production
seed installation.

## Distinguish linear predictions from plant observations

The offline scaled comparison reuses the frozen residual and Jacobian. Column
normalization and one global fraction to the bound are numerical design choices;
they do not reweight the residual, clip coordinates independently or execute a
nonlinear trial. Phase-change caveats remain attached to the predictions.

Local Trial 1 subsequently compared all six frozen directions with nonlinear
plant observations, repeated identically twice. H2 then used a bounded active-set
subproblem and achieved further real descent. Its terminal phase crossings and
an improving small derivative probe show why failure of four tested step fractions
is not evidence of physical infeasibility. A further investigation needs its own
step-size policy and bounded protocol; returned evidence is never rewritten to
make an executed method appear to have tried more than it did.

H3 added controlled local radii, phase-compatible derivative stencils and deeper
backtracking, achieving further real descent without a root. Its terminal model
also demonstrates that decreasing the sum of squared residuals can increase the
maximum residual used for acceptance. A new numerical design must address the
actual acceptance objective; smaller steps alone do not fix this conflict.
The minimax review optimizes along fixed local directions and retains the original
residual scales. Its linear improvements still require separate physical trials.

The H4–H7 evidence separates three further questions: whether a primary minimax
LP passes numerical certificates, whether a secondary choice of smaller changes
is numerically stable, and whether the resulting direction follows the actual
phase path of the plant recipe. A change to the secondary optimization cap is
recorded separately from unchanged physical residual and envelope criteria.
H6 certificates passed while proposed plant steps worsened after a phase change
during preconditioning. Raw-state phase checks alone are insufficient.

Node ownership can close while equilibrium fails: the recorded control-out and
turbine-inlet energy accumulations equal their incoming-minus-outgoing transfers.
These closure checks verify evidence consistency; they do not replace stationarity
criteria. A liquid initial point that returns to mixture during an unconstrained
search does not establish infeasibility of the liquid branch.

The subsequent phase-trajectory trial verified that constraining all four
hotwell checkpoints can produce actual descent. H8 accepted coupled steps but
eventually reached a reported zero quality, where a strictly interior-quality
guard no longer applies. A clipped quality is not a signed distance from the
resolver's domain boundary. Volume reachability alone is also insufficient:
the existing energy-root tolerance participates in boundary acceptance.

The local energy-boundary review reproduces the observed phases in frozen
Trials 6–8 using the authenticated prefix and the resolver's existing tolerance.
This sampled agreement is not global equivalence to the inverse resolver and
does not qualify an optimizer guard. Unit conversion or auxiliary inequality
normalization must never be presented as residual reweighting or a relaxed
physical criterion.

Repeated directional observations at the H8 boundary also show that preserving
phase labels does not establish a useful first-order residual model. At small
steps, prediction error exceeds the observed improvement and the smallest
tested step reverses the descent. Before another search, compare admissible
derivative stencils at several widths, together with energy margins and joint
directional predictions. Identical repeats establish deterministic observations;
they do not establish derivative accuracy.

## Separate readable documentation from validator inputs

`docs/` explains the project, model and current conclusions. Exact-text M10 inputs
belong under `eng/contracts/m10`, where their hashes and original paths are indexed.
Historical validators read frozen navigation snapshots there; current validation
uses the current checkpoint. This lets guides evolve without carrying old status
phrases solely to satisfy earlier scripts.
