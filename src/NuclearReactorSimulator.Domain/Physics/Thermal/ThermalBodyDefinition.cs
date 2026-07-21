using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Thermal;

/// <summary>
/// Immutable definition of a lumped thermal mass with constant heat capacity.
/// </summary>
public sealed record ThermalBodyDefinition
{
    public ThermalBodyDefinition(string id, HeatCapacity heatCapacity)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Thermal-body id cannot be empty.", nameof(id));
        }

        if (heatCapacity <= HeatCapacity.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(heatCapacity), heatCapacity, "Thermal-body heat capacity must be greater than zero.");
        }

        Id = id;
        HeatCapacity = heatCapacity;
    }

    public string Id { get; }

    public HeatCapacity HeatCapacity { get; }
}
