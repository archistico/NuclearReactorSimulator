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

## Separate readable documentation from validator inputs

`docs/` explains the project, model and current conclusions. Exact-text M10 inputs
belong under `eng/contracts/m10`, where their hashes and original paths are indexed.
Historical validators read frozen navigation snapshots there; current validation
uses the current checkpoint. This lets guides evolve without carrying old status
phrases solely to satisfy earlier scripts.
