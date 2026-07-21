namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Indicates that a balance would remove all fluid mass from a node or drive it negative.
/// </summary>
public sealed class FluidNodeDepletionException : InvalidOperationException
{
    public FluidNodeDepletionException(string nodeId, double candidateMassKilograms)
        : base($"Fluid node '{nodeId}' would reach invalid mass {candidateMassKilograms:R} kg. A fluid node must retain positive mass.")
    {
        NodeId = nodeId;
        CandidateMassKilograms = candidateMassKilograms;
    }

    public string NodeId { get; }

    public double CandidateMassKilograms { get; }
}
