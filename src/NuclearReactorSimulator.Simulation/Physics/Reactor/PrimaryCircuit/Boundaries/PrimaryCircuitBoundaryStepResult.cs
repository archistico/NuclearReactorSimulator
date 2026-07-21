using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

public sealed record PrimaryCircuitBoundaryStepResult(
    PrimaryCircuitBoundarySystemSnapshot Snapshot,
    PlantNetworkSourceTerms SourceTerms);
