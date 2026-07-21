using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core;

/// <summary>
/// Immutable local diagnostic snapshot for one aggregated core zone.
/// </summary>
public sealed record CoreZoneSnapshot(
    string ZoneId,
    CoreZoneCoordinate Coordinate,
    CoreZonePowerFraction PowerFraction,
    Power FissionThermalPower,
    Temperature FuelTemperature,
    Temperature StructureTemperature,
    Temperature CoolantTemperature,
    Pressure CoolantPressure,
    FluidPhase CoolantPhase,
    VaporQuality? VaporQuality,
    VoidFraction? VoidFraction);
