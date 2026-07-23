using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Electrical;

/// <summary>
/// Lumped synchronous generator coupled one-to-one to an M4.2 turbine rotor.
/// </summary>
public sealed class SynchronousGeneratorDefinition
{
    public SynchronousGeneratorDefinition(
        string id,
        string rotorId,
        string breakerId,
        int polePairs,
        ElectricPotential ratedLineVoltage,
        Power maximumElectricalPower,
        GeneratorEfficiency efficiency,
        Frequency maximumSynchronizationFrequencyDifference,
        PhaseAngleDifference maximumSynchronizationPhaseDifference,
        ElectricPotential maximumSynchronizationVoltageDifference,
        SynchronousGridCouplingDefinition? gridCoupling = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Synchronous generator id cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(rotorId))
        {
            throw new ArgumentException("Synchronous generator rotor id cannot be empty or whitespace.", nameof(rotorId));
        }

        if (string.IsNullOrWhiteSpace(breakerId))
        {
            throw new ArgumentException("Generator breaker id cannot be empty or whitespace.", nameof(breakerId));
        }

        if (polePairs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(polePairs), polePairs, "Synchronous generator pole-pair count must be greater than zero.");
        }

        if (ratedLineVoltage <= ElectricPotential.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ratedLineVoltage), ratedLineVoltage, "Generator rated line voltage must be greater than zero.");
        }

        if (maximumElectricalPower <= Power.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumElectricalPower), maximumElectricalPower, "Generator maximum electrical power must be greater than zero.");
        }

        if (efficiency.Fraction <= 0d || efficiency.Fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiency), efficiency, "Generator efficiency must be greater than zero and no greater than one.");
        }

        Id = id.Trim();
        RotorId = rotorId.Trim();
        BreakerId = breakerId.Trim();
        PolePairs = polePairs;
        RatedLineVoltage = ratedLineVoltage;
        MaximumElectricalPower = maximumElectricalPower;
        Efficiency = efficiency;
        MaximumSynchronizationFrequencyDifference = maximumSynchronizationFrequencyDifference;
        MaximumSynchronizationPhaseDifference = maximumSynchronizationPhaseDifference;
        MaximumSynchronizationVoltageDifference = maximumSynchronizationVoltageDifference;
        GridCoupling = gridCoupling;
    }

    public string Id { get; }

    public string RotorId { get; }

    public string BreakerId { get; }

    public int PolePairs { get; }

    public ElectricPotential RatedLineVoltage { get; }

    public Power MaximumElectricalPower { get; }

    public GeneratorEfficiency Efficiency { get; }

    public Frequency MaximumSynchronizationFrequencyDifference { get; }

    public PhaseAngleDifference MaximumSynchronizationPhaseDifference { get; }

    public ElectricPotential MaximumSynchronizationVoltageDifference { get; }

    /// <summary>
    /// Optional infinite-bus synchronizing correction. Null preserves the historical dispatch-torque-only model.
    /// </summary>
    public SynchronousGridCouplingDefinition? GridCoupling { get; }

    public Frequency ElectricalFrequencyAt(AngularSpeed mechanicalAngularSpeed)
        => Frequency.FromHertz(PolePairs * mechanicalAngularSpeed.RadiansPerSecond / (2d * Math.PI));
}
