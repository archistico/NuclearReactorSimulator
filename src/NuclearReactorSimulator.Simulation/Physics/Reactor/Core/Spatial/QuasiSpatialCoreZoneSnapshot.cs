using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Spatial;

/// <summary>
/// Immutable M9.4 diagnostic for one aggregated zone. Values are derived from committed canonical plant state and the
/// committed/candidate aggregated-core power-share state; no local neutron population or conserved inventory is owned here.
/// </summary>
public sealed record QuasiSpatialCoreZoneSnapshot(
    string ZoneId,
    CoreZonePowerFraction CommittedPowerFraction,
    CoreZonePowerFraction CandidatePowerFraction,
    Temperature FuelTemperature,
    Temperature CoolantTemperature,
    VoidFraction VoidFraction,
    Reactivity FuelTemperatureReactivity,
    Reactivity CoolantTemperatureReactivity,
    Reactivity VoidReactivity,
    Reactivity LocalFeedbackReactivity,
    Reactivity CoupledShapeDrivingReactivity);
