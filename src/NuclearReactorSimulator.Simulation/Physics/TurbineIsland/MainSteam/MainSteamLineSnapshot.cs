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
    Power InternalEnergyFlowRate);
