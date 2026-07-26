# ADR 0116 — Main-steam header relief is a pressure-actuated external boundary

## Status

Accepted for M10.9.4.1-F.2 candidate validation.

## Context

F.1 validated an isolated ideal-vapor subcritical/choked capacity law but deliberately owned no plant topology. Phase F next requires a conservative load-rejection relief seam without prematurely combining turbine bypass, condenser mixing, receiver inventory, manual authority or the later enthalpy migration.

The current-v2 main-steam header operates near 6.2725 MPa and the validated steam path carries approximately 12 kg/s. F.1 demonstrated 7.880086767 kg/s per 1,000 mm² at the representative state.

## Decision

Add one optional `MainSteamReliefBoundaryDefinition` to `MainSteamNetworkDefinition`.

The current-v2 sustained profiles configure:

- source `header`;
- named external receiver `atmospheric-relief-receiver` at standard atmospheric pressure;
- zero lift at or below 6.5 MPa;
- linear lift to full opening at 6.7 MPa;
- 1,600 mm² full-open throat using the validated F.1 coefficients.

`MainSteamReliefBoundarySolver` reads committed state, applies vapor-availability limiting and emits one source-node removal plus matching signed external mass/power exchange. `MainSteamNetworkSolver` combines those terms before the existing single plant-network integration boundary.

Legacy definitions retain no relief boundaries.

## Consequences

The simulator gains an explicit conservative pressure-relief path without introducing another inventory owner. Relief behavior is inspectable through immutable snapshots and deterministic audit artifacts. The ideal-vapor law cannot discharge subcooled liquid and saturated-mixture capacity is limited by committed vapor quality.

F.2 does not model safety-valve hysteresis, travel time, wet-steam critical flow, discharge piping, receiver thermodynamics, turbine bypass or enthalpy/flow-work transport. Those remain separate decisions.

## Rejected alternatives

### Add relief as an ordinary plant valve to a synthetic atmospheric node

Rejected because the atmospheric receiver would become a finite plant inventory unless a second special boundary owner were added. F.2 instead declares the receiver explicitly as an external boundary.

### Remove header mass directly in the Application runtime

Rejected because Application does not own plant physics or conserved inventory integration.

### Combine relief and turbine bypass in one candidate

Rejected because conservation failures and capacity errors would become difficult to isolate, and condenser backpressure ownership belongs to the later bypass increment.

### Use total mixture mass as unrestricted ideal-vapor capacity

Rejected because that would silently turn the F.1 ideal-vapor seam into an unvalidated two-phase relief model.
