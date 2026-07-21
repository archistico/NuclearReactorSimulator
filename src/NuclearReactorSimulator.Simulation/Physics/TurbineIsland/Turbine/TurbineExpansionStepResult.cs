using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

public sealed record TurbineExpansionStepResult(
    MainSteamNetworkStepResult MainSteamStep,
    TurbineExpansionState CandidateTurbineState,
    TurbineExpansionSnapshot Snapshot)
{
    public PlantState CandidatePlantState => MainSteamStep.CandidateState;
}
