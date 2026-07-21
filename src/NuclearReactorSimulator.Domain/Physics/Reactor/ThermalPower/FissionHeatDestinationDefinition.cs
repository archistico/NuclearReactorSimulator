namespace NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;

/// <summary>
/// Immutable declaration of one destination receiving a fixed fraction of instantaneous fission heat.
/// </summary>
public sealed record FissionHeatDestinationDefinition
{
    public FissionHeatDestinationDefinition(
        string targetDomainId,
        HeatDepositionFraction fraction)
    {
        if (string.IsNullOrWhiteSpace(targetDomainId))
        {
            throw new ArgumentException("Fission-heat target-domain id cannot be empty.", nameof(targetDomainId));
        }

        if (fraction <= HeatDepositionFraction.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fraction),
                fraction,
                "Fission-heat destination fraction must be greater than zero.");
        }

        TargetDomainId = targetDomainId;
        Fraction = fraction;
    }

    public string TargetDomainId { get; }

    public HeatDepositionFraction Fraction { get; }
}
