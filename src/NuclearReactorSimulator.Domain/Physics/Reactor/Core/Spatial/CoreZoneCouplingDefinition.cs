namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core.Spatial;

/// <summary>
/// Symmetric deterministic coupling between two aggregated core zones. Coupling smooths only the quasi-spatial
/// power-shape driving signal; it does not introduce neutron transport or a second kinetics state.
/// </summary>
public sealed record CoreZoneCouplingDefinition
{
    public CoreZoneCouplingDefinition(
        string firstZoneId,
        string secondZoneId,
        CoreZoneCouplingFraction couplingFraction)
    {
        FirstZoneId = ValidateId(firstZoneId, nameof(firstZoneId));
        SecondZoneId = ValidateId(secondZoneId, nameof(secondZoneId));
        if (string.Equals(FirstZoneId, SecondZoneId, StringComparison.Ordinal))
        {
            throw new ArgumentException("A core-zone coupling must connect two distinct zones.", nameof(secondZoneId));
        }

        CouplingFraction = couplingFraction;
    }

    public string FirstZoneId { get; }

    public string SecondZoneId { get; }

    public CoreZoneCouplingFraction CouplingFraction { get; }

    public bool Connects(string zoneId)
        => string.Equals(FirstZoneId, zoneId, StringComparison.Ordinal)
            || string.Equals(SecondZoneId, zoneId, StringComparison.Ordinal);

    public string GetOtherZoneId(string zoneId)
    {
        if (string.Equals(FirstZoneId, zoneId, StringComparison.Ordinal))
        {
            return SecondZoneId;
        }

        if (string.Equals(SecondZoneId, zoneId, StringComparison.Ordinal))
        {
            return FirstZoneId;
        }

        throw new KeyNotFoundException($"Zone '{zoneId}' is not part of this coupling.");
    }

    private static string ValidateId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Core-zone coupling id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
