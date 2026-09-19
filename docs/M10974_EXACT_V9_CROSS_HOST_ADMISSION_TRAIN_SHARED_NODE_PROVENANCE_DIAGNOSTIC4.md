# M10.9.7.4 Exact-V9 Cross-Host Admission-Train Shared-Node Provenance Diagnostic 4

## Authority

Diagnostic/evidence only. No production repair, physics/tolerance change, golden re-anchor, workflow change, VR2/R3 modification, or Repair Planning authority.

## Returned Diagnostic 3 adjudication

Diagnostic 3 is `PASS-AS-AUTHORED`.

At exact-v9 logical step 126, the production stage mass-flow resolver is admission-train limited and the STOP valve is the limiting valve on both hosts.

The production STOP-valve snapshot mass flow is bit-identical to the observed stage commanded mass flow on each host:

- local: `13.339237101974689 kg/s`, bits `402AADB07C452468`
- hosted: `13.339237101974723 kg/s`, bits `402AADB07C45247B`

STOP-valve effective position, flow coefficient, base resistance, coefficient-squared and effective resistance are cross-host bit-identical. The first captured STOP-path difference is therefore its pressure difference:

- local `177935.24646269809 Pa`, bits `4105B879F8C16F60`
- hosted `177935.24646269903 Pa`, bits `4105B879F8C16F80`
- absolute delta `+9.313225746154785E-10 Pa`

The CONTROL-valve pressure difference moves by the exact opposite amount and ADMISSION is bit-identical. Reconstructing the serial admission-train pressure chain from the production snapshots yields:

- turbine inlet: cross-host bit-identical
- control-out: cross-host bit-identical
- stop-out / control-in: hosted is `9.313225746154785E-10 Pa` lower, one ULP at this pressure scale
- header: cross-host bit-identical

This localizes the first captured admission-train state drift to the shared `stop-out` / CONTROL-inlet pressure node. It does not yet prove whether the node's conserved inventory or other thermodynamic properties differ.

## Diagnostic 4 question

Diagnostic 4 asks whether the `stop-out` pressure difference is an isolated pressure-closure difference or whether the production energy-transport outputs already imply different upstream specific internal energy, flow work (`p/rho`), inferred density or transported specific energy.

The gate uses only already-produced `MainSteamValveSnapshot` values from the same immutable step-126 snapshot. It does not capture a pre-step state and does not replay the simulation.

For positive valve flow, `OpenControlVolumeEnergyTransportSolver` uses the from-node as upstream and production computes:

- `internalEnergyRate = u * q`
- `flowWorkRate = (p/rho) * q`
- `enthalpyRate = (u + p/rho) * q`

Diagnostic-side per-kilogram values are explanatory reconstructions from those production rates and the production valve mass flow.

## Perturbation control

The 128-step loop is unchanged from Diagnostic 3. The only in-loop causal action remains the already-qualified immutable step-126 snapshot reference captured after that step has been simulated, serialized and fingerprinted.

All Diagnostic 4 pressure-chain and energy provenance arithmetic is post-loop.

## Capture

Diagnostic 4 adds four one-step files:

- `stage-shared-node-selector.tsv`
- `stage-shared-node-direct.tsv`
- `stage-shared-node-energy-selector.tsv`
- `stage-shared-node-energy-direct.tsv`

The shared-node file records the production valve pressure differences and reconstructs `control-out`, `stop-out` and `header` pressure from the turbine-inlet pressure, including IEEE-754 bits and a chain-closure residual.

The energy file records, for STOP, CONTROL and ADMISSION:

- production mass flow;
- internal-energy flow rate;
- flow-work rate;
- advected-energy flow rate;
- energy-transport mode;
- reconstructed upstream pressure;
- derived specific internal energy;
- derived specific flow work;
- derived specific advected energy;
- inferred density from `p/(p/rho)`;
- enthalpy-identity residual;

all with IEEE-754 bit patterns.

## Decision rule

No repair is authorized by this gate.

- If CONTROL-derived stop-out specific internal energy and inferred density are bit-identical while reconstructed stop-out pressure alone differs, ownership narrows to stop-out pressure/thermodynamic closure.
- If specific internal energy differs first, ownership moves to stop-out conserved-energy / inverse-closure provenance.
- If inferred density or `p/rho` differs with identical specific internal energy, ownership moves to stop-out density/pressure closure.
- If the energy-rate evidence cannot distinguish inventory from inverse-closure arithmetic, a later dedicated diagnostic may capture the pre-step state, but only after explicitly controlling for JIT perturbation.

R3 remains RED and frozen.
