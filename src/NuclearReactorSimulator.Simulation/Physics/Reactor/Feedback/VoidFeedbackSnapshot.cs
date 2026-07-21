using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;

/// <summary>
/// Immutable diagnostic projection of one evaluated void-reactivity feedback.
/// </summary>
public sealed record VoidFeedbackSnapshot(
    string Id,
    VoidFraction ReferenceVoidFraction,
    VoidFraction MeasuredVoidFraction,
    VoidFractionDifference VoidFractionDifference,
    VoidReactivityCoefficient Coefficient,
    Reactivity Reactivity)
{
    public ReactivityContributionKind Kind => ReactivityContributionKind.Void;

    public ReactivityContribution ToContribution() => new(Id, Kind, Reactivity);
}
