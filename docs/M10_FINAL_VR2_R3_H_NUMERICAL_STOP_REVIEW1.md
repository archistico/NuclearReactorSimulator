# R3 H Numerical Stop Review 1

NRS-MARKER:R3-H-NUMERICAL-STOP-REVIEW1

The current execution status and continuation rule live in [PROJECT.md](PROJECT.md).
This gate reviews the frozen H evidence and plans the next numerical investigation.
It performs no plant evaluation, changes no source/test state and reruns no search.

## Evidence boundary

NRS-MARKER:R3-H-NUMERICAL-STOP-REVIEW1-EVIDENCE

The input is the complete byte-authenticated H trace, restored in memory from its
lossless gzip payload. Every stored/logical digest and the original execution
manifest are checked. Production and all 406 current test files are independently
hashed with ordinal repository-relative ordering. Neither existing harness nor
frozen execution contract is edited to reproduce an observation.

The accepted terminal point is event 401, at iteration 7. Iteration 8 records all
48 valid central-difference probes and six sets of 13 trial steps (full step plus
12 halvings). All 78 trials are rejected by the +/-0.02 coordinate box before
the plant evaluator runs. The final trial in every damping attempt violates only
coordinate 21: raw suction internal energy. The point has fractional coordinate
`-0.019998404362641452`, leaving about `1.596e-6` before the lower bound.

Thus the terminal trace contains no valid nonlinear trial that failed to improve
the objective. Its stopping label combines line-search/damping exhaustion, but its
actual final rejection mechanism is the bound check. This is evidence about the
search direction, not evidence that a fluid equilibrium is impossible in the box.

## Offline arithmetic replay

NRS-MARKER:R3-H-NUMERICAL-STOP-REVIEW1-REPLAY

Reconstruct the 75-by-24 Jacobian from the frozen plus/minus residuals using the
authored `2e-6` denominator. Reconstruct damping history from the seven accepted
updates, then reproduce each of the six terminal normal equations, including the
authored summation order, pivot guard and original ordinal pivot tie-breaking.
The independently replayed full coordinates must agree with the recorded coordinates
within an absolute `1e-12` arithmetic comparison guard. This checks a local linear
replay; it does not revise any fluid/R3 acceptance threshold.

For each damping value, compare every diagonal to the zero-damping reconstruction
bit-for-bit. Also compare the six actual recorded full trial vectors bit-for-bit.
Report Jacobian column-norm range and any phase differences in derivative probes;
do not infer a unique cause from a condition-number estimate or from phase labels.

The local replay reproduces every recorded full coordinate with zero error. All six
reconstructed matrices are bit-identical; every diagonal is also bit-identical to
the zero-damping construction. The authored terminal damping range (about 1e-10 to
1e-5) has no represented effect on this normal matrix. Three finite-difference probes
change a phase label, so the replay must not be presented as proof of a smooth physical
Jacobian. These observations describe this frozen point and summation order only.

For the fixed final direction only, compute fractional steps at halvings 0-20
without invoking thermodynamics. Report which remain inside the original box and
the linear-model prediction `r + J * alpha * delta`. An admissible arithmetic point
or a predicted decrease is not an evaluated nonlinear candidate. The historical
12-halving execution and its inconclusive result remain unchanged.
The first box-admissible fraction on this particular direction occurs at halving 15.
Its linear prediction remains above seven million scaled units; no nonlinear
improvement, feasibility or success of a longer line search has been measured.

## Next gate: bounded scaled local-model design, offline only

NRS-MARKER:R3-H-BOUNDED-SCALED-LOCAL-MODEL-DESIGN1-PLAN

The next activity is `H-BOUNDED-SCALED-LOCAL-MODEL-DESIGN1-OFFLINE`. It may implement
a numerical comparison using only the same event-401 residual, frozen Jacobian and
coordinate box. It may not import/call the plant harness or add new physical probes.
It must retain the 75 residual definitions/scales and all 24 existing unknowns.

Freeze the comparison as follows before execution:

1. Keep the recorded unscaled direction as the historical comparator.
2. Compute each Jacobian column's Euclidean norm `s[j]`. For a zero column, keep
   that coordinate's delta at zero; otherwise use `A[:,j] = J[:,j] / s[j]`.
   This is a change of numerical coordinates, not a change in physical residual weights.
3. For each fixed dimensionless damping in `1e-3, 1e-2, 1e-1, 1, 10, 100`, solve
   `(A-transpose A + damping I) z = -A-transpose r`, then recover
   `delta[j] = z[j] / s[j]`. Use largest-absolute partial pivoting, original ordinal
   tie-breaking and the same normalized `1e-14` pivot guard; failed solves are reported.
4. Compute the largest nonnegative fraction to the existing box along that direction:
   the minimum of `(upper-x[j])/delta[j]` for positive deltas and
   `(lower-x[j])/delta[j]` for negative deltas. For an unconstrained zero direction
   use one; for a nonpositive feasible fraction, report no interior trial.
5. Use a single global fraction `min(1, 0.99 * fraction_to_box)`. Do not clip individual
   coordinates or widen the box. Record both uncapped and capped directions and their
   linear-model predictions. Check actual binary64 capped coordinates against the box.
6. Report whether the capped step strictly reduces the same maximum absolute scaled
   linear residual. No predicted decrease is sufficient to authorize a physical run
   or seed promotion. Test the coordinate transform on known coupled linear fixtures,
   a zero column, a badly scaled system and an outward direction near a box boundary.
   Carry the derivative-probe phase-change tags into the report; conditioning changes
   do not validate the local Jacobian's physical smoothness.

This design isolates column scaling and explicit feasible-fraction handling from the
physical model. It does not select a new full nonlinear search, restart H1, increase
the iteration/evaluation/halving budget, or prove constrained stationarity. Any future
nonlinear comparison needs a separately frozen contract and preexecution review.
No active-set heuristic, extra trim variables, controller-memory change or C/J branch
is silently introduced by the local-model experiment.

## Authority and validation

NRS-MARKER:R3-H-NUMERICAL-STOP-REVIEW1-AUTHORITY

```powershell
.\scripts\run-r3-h-numerical-stop-review1.cmd
```

The standard-library-only review emits provenance, terminal observations, damping
replay CSV, fractional-step model CSV, review summary and a SHA-256 manifest under
`artifacts/r3-h-numerics1`. Its tests/production identity proof preserves the preceding
ordinary/current-evidence PASS; this gate claims no new execution of that suite.

R3 remains RED. Bounds, seed, model, gains, payload, final envelopes, canonical
Exact-V9, Requalification 3 and R4 authority remain unchanged. The only next permission
is implementation and validation of the offline local-model design above.
