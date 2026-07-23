using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Immutable centrifugal-pump definition composed over a passive hydraulic path.
/// The active pressure source follows the speed-squared affinity law; internal resistance
/// represents the quadratic droop of the simplified pump curve.
/// </summary>
public sealed record PumpDefinition
{
    public PumpDefinition(
        string id,
        PipeDefinition pipe,
        PressureDifference ratedPressureBoost,
        QuadraticHydraulicResistance internalResistance,
        PumpEfficiency efficiency,
        bool hasDischargeCheckValve = false)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A pump identifier cannot be empty or whitespace.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(pipe);

        if (ratedPressureBoost.Pascals <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(ratedPressureBoost), ratedPressureBoost, "Rated pump pressure boost must be greater than zero.");
        }

        if (internalResistance.PascalSecondsSquaredPerKilogramSquared <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(internalResistance), internalResistance, "Pump internal hydraulic resistance must be greater than zero.");
        }

        if (efficiency.Fraction <= 0d || efficiency.Fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiency), efficiency, "Pump efficiency must be greater than zero and no greater than one.");
        }

        Id = id.Trim();
        Pipe = pipe;
        RatedPressureBoost = ratedPressureBoost;
        InternalResistance = internalResistance;
        Efficiency = efficiency;
        HasDischargeCheckValve = hasDischargeCheckValve;
    }

    public string Id { get; }

    public PipeDefinition Pipe { get; }

    public PressureDifference RatedPressureBoost { get; }

    public QuadraticHydraulicResistance InternalResistance { get; }

    public PumpEfficiency Efficiency { get; }

    /// <summary>
    /// When true, a non-return valve on the pump discharge prevents hydraulic flow opposite to the pump path's
    /// reference direction. The check valve is passive: it does not create forward flow and it remains effective
    /// whether the pump rotor is running or stopped.
    /// </summary>
    public bool HasDischargeCheckValve { get; }
}
