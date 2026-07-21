using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

/// <summary>
/// Per-step controllable feedwater boundary input.
/// The incoming thermodynamic condition is represented by specific internal energy at the modeled boundary.
/// </summary>
public sealed record FeedwaterBoundaryInput
{
    public FeedwaterBoundaryInput(
        string boundaryId,
        MassFlowRate massFlowRate,
        SpecificEnergy specificInternalEnergy)
    {
        if (string.IsNullOrWhiteSpace(boundaryId))
        {
            throw new ArgumentException("Feedwater boundary id cannot be empty or whitespace.", nameof(boundaryId));
        }

        if (massFlowRate < MassFlowRate.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(massFlowRate), massFlowRate, "Feedwater mass flow cannot be negative.");
        }

        if (specificInternalEnergy < SpecificEnergy.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specificInternalEnergy),
                specificInternalEnergy,
                "Feedwater specific internal energy cannot be negative.");
        }

        BoundaryId = boundaryId.Trim();
        MassFlowRate = massFlowRate;
        SpecificInternalEnergy = specificInternalEnergy;
    }

    public string BoundaryId { get; }

    public MassFlowRate MassFlowRate { get; }

    public SpecificEnergy SpecificInternalEnergy { get; }
}
