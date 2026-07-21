using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;

public sealed record IntegratedPrimaryCircuitStepResult(
    PlantNetworkStepResult NetworkStep,
    IntegratedPrimaryCircuitSnapshot Snapshot)
{
    public PlantState CandidateState => NetworkStep.CandidateState;
}
