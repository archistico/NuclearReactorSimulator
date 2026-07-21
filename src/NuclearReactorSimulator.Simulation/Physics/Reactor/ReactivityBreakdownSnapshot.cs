using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor;

/// <summary>
/// Immutable diagnostic projection of the canonical reactivity breakdown.
/// </summary>
public sealed class ReactivityBreakdownSnapshot
{
    private readonly ReadOnlyCollection<ReactivityContribution> _contributions;

    internal ReactivityBreakdownSnapshot(
        Reactivity total,
        ReactivityContribution[] canonicalContributions)
    {
        Total = total;
        _contributions = Array.AsReadOnly(canonicalContributions);
    }

    public Reactivity Total { get; }

    public IReadOnlyList<ReactivityContribution> Contributions => _contributions;

    public Reactivity TotalFor(ReactivityContributionKind kind)
    {
        if (!Enum.IsDefined(typeof(ReactivityContributionKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown reactivity contribution kind.");
        }

        return ReactivityModel.SumCanonical(
            _contributions.Where(contribution => contribution.Kind == kind));
    }
}
