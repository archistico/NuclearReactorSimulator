using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

/// <summary>
/// Educational pressure/temperature-dependent turbine-work closure.
/// The model uses an idealized vapor expansion estimate, bounded by the stage design work and by a configured
/// fraction of committed inlet internal energy. It is intentionally replaceable and is not an engineering steam-table model.
/// </summary>
public sealed class TurbineThermodynamicWorkDefinition
{
    public TurbineThermodynamicWorkDefinition(
        SpecificHeatCapacity vaporSpecificHeatAtConstantPressure,
        double heatCapacityRatio,
        double maximumInletInternalEnergyExtractionFraction)
    {
        if (vaporSpecificHeatAtConstantPressure <= SpecificHeatCapacity.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vaporSpecificHeatAtConstantPressure),
                vaporSpecificHeatAtConstantPressure,
                "Turbine vapor specific heat must be greater than zero.");
        }

        if (!double.IsFinite(heatCapacityRatio) || heatCapacityRatio <= 1d || heatCapacityRatio > 2d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heatCapacityRatio),
                heatCapacityRatio,
                "Turbine vapor heat-capacity ratio must be finite, greater than one and no greater than two.");
        }

        if (!double.IsFinite(maximumInletInternalEnergyExtractionFraction)
            || maximumInletInternalEnergyExtractionFraction <= 0d
            || maximumInletInternalEnergyExtractionFraction > 0.8d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumInletInternalEnergyExtractionFraction),
                maximumInletInternalEnergyExtractionFraction,
                "Maximum turbine inlet-internal-energy extraction fraction must be finite, greater than zero and no greater than 0.8.");
        }

        VaporSpecificHeatAtConstantPressure = vaporSpecificHeatAtConstantPressure;
        HeatCapacityRatio = heatCapacityRatio;
        MaximumInletInternalEnergyExtractionFraction = maximumInletInternalEnergyExtractionFraction;
    }

    public SpecificHeatCapacity VaporSpecificHeatAtConstantPressure { get; }

    public double HeatCapacityRatio { get; }

    /// <summary>
    /// Numerical/physical reserve that prevents the current model from attempting to remove all committed inlet internal energy.
    /// The current-v2 value is deliberately capped at 0.8 so normal overspeed transients remain energy-bounded before protection acts.
    /// </summary>
    public double MaximumInletInternalEnergyExtractionFraction { get; }
}
