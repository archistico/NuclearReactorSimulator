using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Electrical;

/// <summary>
/// M4.5 infinite-bus grid boundary used for manual synchronization training.
/// </summary>
public sealed class ElectricalGridDefinition
{
    public ElectricalGridDefinition(string id, Frequency nominalFrequency, ElectricPotential nominalLineVoltage)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Electrical grid id cannot be empty or whitespace.", nameof(id));
        }

        if (nominalFrequency <= Frequency.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(nominalFrequency), nominalFrequency, "Grid nominal frequency must be greater than zero.");
        }

        if (nominalLineVoltage <= ElectricPotential.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(nominalLineVoltage), nominalLineVoltage, "Grid nominal line voltage must be greater than zero.");
        }

        Id = id.Trim();
        NominalFrequency = nominalFrequency;
        NominalLineVoltage = nominalLineVoltage;
    }

    public string Id { get; }

    public Frequency NominalFrequency { get; }

    public ElectricPotential NominalLineVoltage { get; }
}
