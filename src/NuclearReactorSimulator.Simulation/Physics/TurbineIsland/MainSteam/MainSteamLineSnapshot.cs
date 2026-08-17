using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

public sealed record MainSteamLineSnapshot(
    string LineId,
    string SteamExportBoundaryId,
    string PipeId,
    string SourceNodeId,
    string HeaderNodeId,
    PressureDifference PressureDifference,
    MassFlowRate MassFlowRate,
    Power InternalEnergyFlowRate,
    Power FlowWorkRate,
    Power AdvectedEnergyFlowRate,
    FluidEnergyTransportMode EnergyTransportMode);
