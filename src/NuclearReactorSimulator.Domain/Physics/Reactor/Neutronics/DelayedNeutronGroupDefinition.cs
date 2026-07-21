using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;

/// <summary>
/// Immutable delayed-neutron group parameters for the point-kinetics model.
/// </summary>
public sealed record DelayedNeutronGroupDefinition
{
    public DelayedNeutronGroupDefinition(
        string id,
        DelayedNeutronFraction fraction,
        DecayConstant decayConstant)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Delayed-neutron group id is required.", nameof(id))
            : id.Trim();

        if (fraction.Fraction <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Delayed-neutron group fraction must be greater than zero.");
        }

        if (decayConstant.PerSecond <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(decayConstant), decayConstant, "Decay constant must be greater than zero.");
        }

        Fraction = fraction;
        DecayConstant = decayConstant;
    }

    public string Id { get; }

    public DelayedNeutronFraction Fraction { get; }

    public DecayConstant DecayConstant { get; }
}
