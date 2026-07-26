using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

public sealed record MainSteamReliefBoundarySnapshot(
    string BoundaryId,
    string SourceHeaderNodeId,
    string ReceiverBoundaryId,
    Pressure SourcePressure,
    Temperature SourceTemperature,
    FluidPhase SourcePhase,
    VaporQuality? SourceVaporQuality,
    Pressure ReceiverPressure,
    double LiftFraction,
    double VaporAvailabilityFraction,
    Area EffectiveThroatArea,
    bool IsChoked,
    MassFlowRate MassFlowRate,
    SpecificEnergy ExportedSpecificInternalEnergy,
    Power EnergyExportRate);
