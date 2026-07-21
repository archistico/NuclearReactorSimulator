using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Channels;

/// <summary>Immutable diagnostics for one equivalent fuel-channel group.</summary>
public sealed record FuelChannelGroupSnapshot(
    string GroupId,
    string ZoneId,
    int RepresentedChannelCount,
    CoreZonePowerFraction ZonePowerFraction,
    Power FissionThermalPower,
    Power DecayHeatPower,
    Power TotalNuclearHeatPower,
    Power PerChannelFissionThermalPower,
    Power PerChannelTotalNuclearHeatPower,
    MassFlowRate MassFlowRate,
    MassFlowRate PerChannelMassFlowRate,
    PressureDifference HydraulicPressureDifference,
    Temperature InletCoolantTemperature,
    Temperature OutletCoolantTemperature,
    Pressure InletCoolantPressure,
    Pressure OutletCoolantPressure,
    FluidPhase OutletCoolantPhase,
    VaporQuality? OutletVaporQuality,
    VoidFraction? OutletVoidFraction,
    Power FuelHeatPower,
    Power StructureHeatPower,
    Power CoolantHeatPower);
