# M10.9.7.4 Exact-V9 Cross-Host Stage Mass-Flow Resolver Causal-Seam Diagnostic 3

## Authority

Diagnostic/evidence only. No production repair, physics/tolerance change, golden re-anchor, workflow change, VR2/R3 modification, or Repair Planning authority.

## Returned Diagnostic 2 adjudication

Diagnostic 2 REV1 is `PASS-AS-AUTHORED`.

The local and hosted traces agree on phase policy, trip state, inlet phase/quality, resolved admission vapor mass fraction, inlet pressure/temperature/internal energy, exhaust pressure/temperature, rotor speeds, external load and passive loss. The effective-flow identity residual is exactly zero on both hosts.

The first upstream divergent value captured by Diagnostic 2 is already `TurbineStageGroupSnapshot.CommandedMassFlowRate` at logical step 126:

- local: `13.339237101974689 kg/s`, bits `402AADB07C452468`
- hosted: `13.339237101974723 kg/s`, bits `402AADB07C45247B`
- absolute delta: `3.375077994860476E-14 kg/s`
- ULP distance: `19`

Therefore vapor-fraction multiplication is not the origin. It only propagates a pre-existing resolver delta.

## Diagnostic 3 question

`TurbineStageMassFlowResolver.ResolvePressureDrivenExpansionFlow` selects:

`min(pressureDrivenCandidate, admissionTrainCandidate)`

where:

- `pressureDrivenCandidate = min(sqrt((inletP - exhaustP) / expansionResistance), 0.5 * inletMass / dt)`
- `admissionTrainCandidate = min(stopPositiveFlow, min(controlPositiveFlow, admissionPositiveFlow))`

Diagnostic 3 determines which exact candidate first differs cross-host and which limiter owns the commanded flow.

The main-steam admission-train snapshots are authoritative evidence for the valve candidates: `MainSteamNetworkSolver` evaluates the same valve definitions with `ValveFlowSolver` from the same commanded plant state passed into the full-plant step. Diagnostic-side reconstruction of coefficient-squared, effective resistance and square-root flow is explanatory evidence only; limiter selection is based on the production `MainSteamValveSnapshot.MassFlowRate` values.

## Perturbation control

The 128-step loop is intentionally identical to Diagnostic 2 REV1: the only causal reference capture is the already-qualified immutable step-126 snapshot, and it happens after the step-126 payload has been serialized and fingerprinted. No new pre-step branch or assignment is added. All resolver traversal and arithmetic occur only after all 128 simulation steps are complete.

The drainable pre-step inventory bound is deliberately not reconstructed in this gate because doing so would require new instrumentation before step 126. Diagnostic 3 instead compares the observed commanded flow against the production valve-flow snapshots and the hydraulic sqrt candidate. It also records the pre-step mass that would be required for a drainable bound to tie the visible candidate; any unresolved exact-tie ambiguity must be handled by a later dedicated diagnostic rather than assumed away.

## Capture

The existing transitive and Diagnostic 2 files remain. Diagnostic 3 adds four one-step evidence files:

- `stage-resolver-selector.tsv`
- `stage-resolver-direct.tsv`
- `stage-resolver-valves-selector.tsv`
- `stage-resolver-valves-direct.tsv`

The resolver files record fixed `dt`, post-step inlet mass context, stage driving pressure, expansion resistance, the hydraulic sqrt candidate, the three positive production valve-flow candidates, the visible `min(hydraulic, admissionTrain)` candidate, observed commanded flow, residuals against the visible/admission/hydraulic candidates, the pre-step mass that would be required for a drainable exact tie, and selected visible/valve limiters, all with IEEE-754 bit patterns.

The valve files record valve role/id, characteristic kind/rangeability, effective position, flow coefficient, pressure difference, base/effective resistance, coefficient squared, squared mass flow, reconstructed signed flow, production snapshot flow, positive snapshot flow and reconstruction residual, all with IEEE-754 bit patterns.

## Expected hosted behavior

The frozen Exact-V9 anchor remains unchanged. GitHub hosted `ordinary-ci` is expected to remain RED on that anchor until causal adjudication is complete, while `ordinary-ci-diagnostics` must return the Diagnostic 3 resolver trace.

## Decision rule

No repair is authorized by this gate. The next owner is selected strictly from the first divergent production operand/candidate:

- divergent hydraulic candidate with identical operands -> numerical `sqrt`/division seam;
- divergent valve effective position -> control/actuator seam;
- identical position but divergent flow coefficient -> valve characteristic seam;
- identical coefficient but divergent valve pressure difference -> upstream main-steam hydraulic-state seam;
- identical valve operands but divergent snapshot flow -> `ValveFlowSolver` arithmetic seam.

R3 remains RED and frozen.
