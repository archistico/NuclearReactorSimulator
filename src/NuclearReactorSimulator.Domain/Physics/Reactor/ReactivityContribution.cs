using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor;

/// <summary>
/// One immutable, named contribution to total reactor reactivity.
/// </summary>
public sealed record ReactivityContribution
{
    public ReactivityContribution(
        string id,
        ReactivityContributionKind kind,
        Reactivity value)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Reactivity contribution id cannot be empty.", nameof(id));
        }

        if (!Enum.IsDefined(typeof(ReactivityContributionKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown reactivity contribution kind.");
        }

        Id = id;
        Kind = kind;
        Value = value;
    }

    public string Id { get; }

    public ReactivityContributionKind Kind { get; }

    public Reactivity Value { get; }
}
