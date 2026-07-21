using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;

/// <summary>
/// Immutable destination receiving a fixed fraction of emitted decay heat.
/// </summary>
public sealed record DecayHeatDestinationDefinition
{
    public DecayHeatDestinationDefinition(
        string targetDomainId,
        HeatDepositionFraction fraction)
    {
        if (string.IsNullOrWhiteSpace(targetDomainId))
        {
            throw new ArgumentException("Decay-heat target-domain id cannot be empty.", nameof(targetDomainId));
        }

        if (fraction <= HeatDepositionFraction.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fraction),
                fraction,
                "Decay-heat destination fraction must be greater than zero.");
        }

        TargetDomainId = targetDomainId.Trim();
        Fraction = fraction;
    }

    public string TargetDomainId { get; }

    public HeatDepositionFraction Fraction { get; }
}
