using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

public sealed record MainSteamNetworkStepResult(
    IntegratedPrimaryCircuitStepResult PrimaryCircuitStep,
    MainSteamNetworkSnapshot Snapshot)
{
    public PlantState CandidateState => PrimaryCircuitStep.CandidateState;
}
