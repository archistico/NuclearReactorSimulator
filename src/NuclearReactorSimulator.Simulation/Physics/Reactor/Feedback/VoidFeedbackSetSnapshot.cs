using System.Collections.ObjectModel;
using NuclearReactorSimulator.Simulation.Physics.Reactor;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;

/// <summary>
/// Canonically ordered void-feedback diagnostics plus their composed reactivity breakdown.
/// </summary>
public sealed class VoidFeedbackSetSnapshot
{
    private readonly ReadOnlyCollection<VoidFeedbackSnapshot> _feedbacks;

    internal VoidFeedbackSetSnapshot(
        VoidFeedbackSnapshot[] feedbacks,
        ReactivityBreakdownSnapshot reactivityBreakdown)
    {
        _feedbacks = Array.AsReadOnly(feedbacks);
        ReactivityBreakdown = reactivityBreakdown;
    }

    public IReadOnlyList<VoidFeedbackSnapshot> Feedbacks => _feedbacks;

    public ReactivityBreakdownSnapshot ReactivityBreakdown { get; }
}
