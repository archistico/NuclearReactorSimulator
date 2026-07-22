# Secondary-System Transients — M8.4

M8.4 packages deterministic turbine, generator, feedwater and condenser disturbances over the validated full-plant runtime.

## Transient-ready initial condition

`secondary-transient-ready / v1` reuses the M7 canonical operational recipe:

- warm critical reactor with main circulation established;
- turbine at approximately synchronous speed;
- generator breaker initially closed with a 5 MWe requested load;
- canonical steam/feedwater/condenser topology unchanged;
- finite 0.1 MW condenser cooling-boundary capacity, scaled to the compact 10 m³ educational exhaust steam space so the deterministic seed remains inside the validated water/steam envelope.

The finite cooling capacity is an input boundary only. It is not condenser pressure, vacuum or a second thermal state.

## Turbine trip

`m84-turbine-trip` activates the existing M5.5 turbine-trip path. The protection latch, stop/isolation commands, steam-path response and rotor dynamics remain owned by M5.5/M4.

## Generator trip / load rejection

`m84-generator-trip-load-rejection` activates the existing M5.5 generator-trip path. Breaker opening and removal of electromagnetic loading are consequences of M4.5 ownership. M8.4 never directly sets electrical output or shaft torque.

## Feedwater degradation / loss

`m84-feedwater-loss-degradation` reuses M8.2:

- 35% canonical feedwater-pump capacity during the degradation interval;
- canonical feedwater-pump trip during the loss interval.

Any drum/feedwater/hotwell response is produced by the existing M3/M4 network and M5.4 controls.

## Condenser cooling / vacuum degradation and loss

`m84-condenser-vacuum-degradation-loss` applies:

- 25% of persistent cooling-boundary heat-rejection capacity during degradation;
- 0% during total cooling loss.

Only `CondenserCoolingBoundaryInput.AvailableHeatRejectionPower` is altered. Condensation rate, exhaust inventory, pressure and vacuum remain consequences of the M4.3 condenser solver and conserved plant state.

## Determinism and replay

All transitions use the M8.1 logical-step lifecycle. Re-running the same initial condition and scenario definition with the same operator command trace reproduces the same transition ordering. No wall-clock timing, hidden randomness or UI refresh cadence influences the transient.

## Model limits

These scenarios are educational system transients within the current simplified model. They do not claim detailed turbine-stage maps, excitation/grid electromagnetic transients, feedwater-heater/deaerator dynamics, circulating-water hydraulics, air ingress or ejector physics. M8.5 remains the bounded educational leak/LOCA-class candidate; M8.6 is a stacked candidate for explicit electrical-loss/station-blackout-class extensions.
