using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Thermal;

/// <summary>
/// Immutable conserved energy inventory for one lumped thermal body.
/// Temperature is derived from stored energy and constant heat capacity.
/// </summary>
public sealed record ThermalBodyState
{
    public ThermalBodyState(ThermalBodyDefinition definition, Energy storedThermalEnergy)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (storedThermalEnergy < Energy.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(storedThermalEnergy),
                storedThermalEnergy,
                "Stored thermal energy cannot be negative relative to absolute zero.");
        }

        Definition = definition;
        StoredThermalEnergy = storedThermalEnergy;
    }

    public ThermalBodyDefinition Definition { get; }

    public string Id => Definition.Id;

    public Energy StoredThermalEnergy { get; }

    public Temperature Temperature => Temperature.FromKelvins(
        StoredThermalEnergy.Joules / Definition.HeatCapacity.JoulesPerKelvin);

    public static ThermalBodyState FromTemperature(
        ThermalBodyDefinition definition,
        Temperature temperature)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return new ThermalBodyState(
            definition,
            Energy.FromJoules(definition.HeatCapacity.JoulesPerKelvin * temperature.Kelvins));
    }
}
