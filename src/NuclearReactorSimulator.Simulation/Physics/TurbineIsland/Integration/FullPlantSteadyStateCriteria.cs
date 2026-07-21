namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>Explicit configurable M4.7 acceptance limits for a fixed-input full-plant reference run.</summary>
public sealed record FullPlantSteadyStateCriteria
{
    public FullPlantSteadyStateCriteria(
        double maximumAbsoluteMassInventoryDriftKilograms,
        double maximumAbsoluteCoupledStoredEnergyDriftJoules,
        double maximumAbsoluteRotorSpeedDriftRevolutionsPerMinute,
        double maximumAbsoluteElectricalOutputDriftWatts,
        double maximumAbsoluteMassClosureResidualKilograms,
        double maximumAbsoluteFullEnergyPathClosureResidualJoules)
    {
        MaximumAbsoluteMassInventoryDriftKilograms = NonNegativeFinite(maximumAbsoluteMassInventoryDriftKilograms, nameof(maximumAbsoluteMassInventoryDriftKilograms));
        MaximumAbsoluteCoupledStoredEnergyDriftJoules = NonNegativeFinite(maximumAbsoluteCoupledStoredEnergyDriftJoules, nameof(maximumAbsoluteCoupledStoredEnergyDriftJoules));
        MaximumAbsoluteRotorSpeedDriftRevolutionsPerMinute = NonNegativeFinite(maximumAbsoluteRotorSpeedDriftRevolutionsPerMinute, nameof(maximumAbsoluteRotorSpeedDriftRevolutionsPerMinute));
        MaximumAbsoluteElectricalOutputDriftWatts = NonNegativeFinite(maximumAbsoluteElectricalOutputDriftWatts, nameof(maximumAbsoluteElectricalOutputDriftWatts));
        MaximumAbsoluteMassClosureResidualKilograms = NonNegativeFinite(maximumAbsoluteMassClosureResidualKilograms, nameof(maximumAbsoluteMassClosureResidualKilograms));
        MaximumAbsoluteFullEnergyPathClosureResidualJoules = NonNegativeFinite(maximumAbsoluteFullEnergyPathClosureResidualJoules, nameof(maximumAbsoluteFullEnergyPathClosureResidualJoules));
    }

    public double MaximumAbsoluteMassInventoryDriftKilograms { get; }

    public double MaximumAbsoluteCoupledStoredEnergyDriftJoules { get; }

    public double MaximumAbsoluteRotorSpeedDriftRevolutionsPerMinute { get; }

    public double MaximumAbsoluteElectricalOutputDriftWatts { get; }

    public double MaximumAbsoluteMassClosureResidualKilograms { get; }

    public double MaximumAbsoluteFullEnergyPathClosureResidualJoules { get; }

    private static double NonNegativeFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Steady-state criteria must be finite and non-negative.");
        }

        return value;
    }
}
