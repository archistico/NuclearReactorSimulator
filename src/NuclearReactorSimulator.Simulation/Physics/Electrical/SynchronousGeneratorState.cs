using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

/// <summary>
/// Committed M4.5 electrical state for one synchronous generator and its breaker.
/// </summary>
public sealed record SynchronousGeneratorState
{
    public SynchronousGeneratorState(string generatorId, PhaseAngle electricalPhaseAngle, bool breakerClosed)
    {
        if (string.IsNullOrWhiteSpace(generatorId))
        {
            throw new ArgumentException("Generator-state id cannot be empty or whitespace.", nameof(generatorId));
        }

        GeneratorId = generatorId.Trim();
        ElectricalPhaseAngle = electricalPhaseAngle;
        BreakerClosed = breakerClosed;
    }

    public string GeneratorId { get; }

    public PhaseAngle ElectricalPhaseAngle { get; }

    public bool BreakerClosed { get; }
}
