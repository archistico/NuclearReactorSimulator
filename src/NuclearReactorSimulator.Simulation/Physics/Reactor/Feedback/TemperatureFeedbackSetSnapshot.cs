using System.Collections.ObjectModel;
using NuclearReactorSimulator.Simulation.Physics.Reactor;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;

/// <summary>
/// Canonically ordered temperature-feedback diagnostics plus their composed reactivity breakdown.
/// </summary>
public sealed class TemperatureFeedbackSetSnapshot
{
    private readonly ReadOnlyCollection<TemperatureFeedbackSnapshot> _feedbacks;

    internal TemperatureFeedbackSetSnapshot(
        TemperatureFeedbackSnapshot[] feedbacks,
        ReactivityBreakdownSnapshot reactivityBreakdown)
    {
        _feedbacks = Array.AsReadOnly(feedbacks);
        ReactivityBreakdown = reactivityBreakdown;
    }

    public IReadOnlyList<TemperatureFeedbackSnapshot> Feedbacks => _feedbacks;

    public ReactivityBreakdownSnapshot ReactivityBreakdown { get; }
}
