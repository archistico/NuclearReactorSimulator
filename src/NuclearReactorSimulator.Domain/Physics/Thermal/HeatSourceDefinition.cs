using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Thermal;

/// <summary>
/// Immutable external thermal-power source targeting one named thermal domain.
/// </summary>
public sealed record HeatSourceDefinition
{
    public HeatSourceDefinition(string id, string targetDomainId, Power ratedThermalPower)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Heat-source id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(targetDomainId))
        {
            throw new ArgumentException("Heat-source target-domain id cannot be empty.", nameof(targetDomainId));
        }

        if (ratedThermalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ratedThermalPower),
                ratedThermalPower,
                "Rated heat-source power cannot be negative.");
        }

        Id = id;
        TargetDomainId = targetDomainId;
        RatedThermalPower = ratedThermalPower;
    }

    public string Id { get; }

    public string TargetDomainId { get; }

    public Power RatedThermalPower { get; }
}
