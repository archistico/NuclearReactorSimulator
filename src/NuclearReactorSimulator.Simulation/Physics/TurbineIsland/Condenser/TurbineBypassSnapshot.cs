using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

public sealed record TurbineBypassSnapshot(
    string BypassId,
    string SourceHeaderNodeId,
    string CondenserId,
    string DestinationSteamSpaceNodeId,
    Pressure SourcePressure,
    Temperature SourceTemperature,
    FluidPhase SourcePhase,
    VaporQuality? SourceVaporQuality,
    Pressure DestinationPressure,
    double OpenFraction,
    double VaporAvailabilityFraction,
    Area EffectiveThroatArea,
    bool IsChoked,
    MassFlowRate MassFlowRate,
    FluidEnergyTransportMode EnergyTransportMode,
    SpecificEnergy TransferredSpecificInternalEnergy,
    SpecificEnergy TransferredSpecificFlowWork,
    SpecificEnergy TransferredSpecificEnthalpy,
    Power InternalEnergyTransferRate,
    Power FlowWorkTransferRate,
    Power AdvectedEnergyTransferRate);
