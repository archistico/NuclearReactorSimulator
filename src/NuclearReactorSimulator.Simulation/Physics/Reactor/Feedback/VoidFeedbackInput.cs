using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;

/// <summary>
/// One committed void-fraction reading evaluated against a configured feedback definition.
/// </summary>
public sealed record VoidFeedbackInput
{
    public VoidFeedbackInput(
        VoidReactivityFeedbackDefinition definition,
        VoidFraction voidFraction)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        VoidFraction = voidFraction;
    }

    public VoidReactivityFeedbackDefinition Definition { get; }

    public VoidFraction VoidFraction { get; }
}
