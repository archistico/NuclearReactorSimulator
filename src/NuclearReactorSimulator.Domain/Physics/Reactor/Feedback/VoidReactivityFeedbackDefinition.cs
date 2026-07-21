using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;

/// <summary>
/// Immutable linear void-reactivity feedback definition around an explicit reference void fraction.
/// </summary>
public sealed record VoidReactivityFeedbackDefinition
{
    public VoidReactivityFeedbackDefinition(
        string id,
        VoidFraction referenceVoidFraction,
        VoidReactivityCoefficient coefficient)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Void-feedback id cannot be empty.", nameof(id));
        }

        Id = id;
        ReferenceVoidFraction = referenceVoidFraction;
        Coefficient = coefficient;
    }

    public string Id { get; }

    public ReactivityContributionKind Kind => ReactivityContributionKind.Void;

    public VoidFraction ReferenceVoidFraction { get; }

    public VoidReactivityCoefficient Coefficient { get; }
}
