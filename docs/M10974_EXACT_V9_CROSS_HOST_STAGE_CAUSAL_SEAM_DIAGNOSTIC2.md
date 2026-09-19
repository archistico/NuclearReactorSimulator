# M10.9.7.4 Exact-V9 Cross-Host Stage Causal-Seam Diagnostic 2 REV1

NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-STAGE-CAUSAL-SEAM-DIAGNOSTIC2-DOC
NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-STAGE-CAUSAL-SEAM-DIAGNOSTIC2-REV1-DOC

## Purpose

Diagnostic 1 localized the complete hosted Exact-V9 aggregate mismatch to one step (`126`) and five `turbineSecondary` numeric leaves. Diagnostic 2 REV1 remains test/evidence-only and inspects the stage/rotor seam at that already-frozen divergent step.

REV1 supersedes the unexecuted first Diagnostic 2 candidate. Review found that collecting a full causal record at every one of the 128 steps would add avoidable work between simulation steps and could perturb a few-ULP cross-host effect through tiered-JIT/background-compilation timing. Review also found that `VaporQuality` alone is not the production admission quantity: the solver uses `FluidThermodynamicState.VaporMassFraction`, then applies the admission policy clamp and turbine trip gate.

## Minimal-perturbation capture contract

The 128-step determinism loop keeps the Diagnostic 1 workload unchanged except for one frozen conditional at step 126. After the step-126 presentation payload and fingerprint have already been produced, the test stores only a reference to the immutable canonical snapshot. No turbine traversal, assertion, multiplication, subtraction, string formatting or causal-record allocation occurs in the loop.

After all 128 steps have completed, REV1 traverses the retained step-126 canonical snapshot and emits one selector row and one direct row. This preserves the already-observed step-126 trajectory as closely as possible while still exposing the internal causal seam.

The TSVs are:

- `stage-causal-selector.tsv` — header plus exactly one data row for step 126;
- `stage-causal-direct.tsv` — header plus exactly one data row for step 126.

## Production-semantic mirror

REV1 reconstructs admission vapor mass fraction with the same semantics as `TurbineExpansionSolver.ResolveAdmissionVaporFraction`:

- `LegacyUnrestricted` -> `1.0`;
- `SubcooledLiquid` -> `0.0`;
- `SaturatedMixture` -> `VaporQuality.Fraction`;
- `SuperheatedVapor` -> `1.0`;
- unresolved phase -> `0.0` after null fallback;
- non-legacy value -> `Math.Clamp(value, 0, 1)`.

It then mirrors the effective-flow gate:

`phaseLimitedFlow = commandedFlow * resolvedAdmissionVaporMassFraction`

`expectedEffectiveFlow = tripBlocked ? 0 : phaseLimitedFlow`

The capture records phase-policy identity, trip state, train/stage phase and quality, raw thermodynamic vapor mass fraction, resolved admission vapor mass fraction, commanded/effective/boundary/phase-limited flow, residual, inlet/exhaust thermodynamic quantities, specific work, stage torque/power/moisture drain, and rotor/load terms. Numeric values use invariant `G17` plus IEEE-754 bit patterns.

## Decision rules

1. If commanded stage flow is already different, Diagnostic 2 REV1 selects the stage-flow/main-steam hydraulic input path as the next owner. It does **not** claim the first floating-point operation; a narrower upstream hydraulic-flow diagnostic is then required.
2. If commanded flow is bit-identical but resolved admission vapor mass fraction differs, ownership moves to turbine-inlet thermodynamic closure / phase-quality resolution.
3. If commanded flow and resolved vapor mass fraction are bit-identical but phase-limited/effective flow differs, ownership is the admission multiplication/trip arithmetic seam.
4. If effective flow is bit-identical but specific work, torque or power differs, ownership moves downstream to work/rotor arithmetic.
5. No production repair, golden change, tolerance change, physics retune or VR2/R3 change is authorized by this diagnostic.

## Expected execution

Local execution must remain GREEN on frozen Exact-V9 aggregate `7880AD...B5418` and return the one-row selector/direct causal TSVs. The exact same REV1 candidate is then pushed unchanged. Hosted ordinary CI is expected to remain RED on the frozen Exact-V9 aggregate while returning the REV1 stage-causal trace for adjudication.
