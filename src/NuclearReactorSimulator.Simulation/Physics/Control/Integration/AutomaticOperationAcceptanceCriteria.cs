namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

/// <summary>Observational M5.7 gate limits. They never alter controller, protection, alarm or physical state.</summary>
public sealed record AutomaticOperationAcceptanceCriteria
{
    public AutomaticOperationAcceptanceCriteria(
        double maximumAbsoluteMassClosureResidualKilograms,
        double maximumAbsoluteFullEnergyPathClosureResidualJoules,
        int maximumInvalidMeasuredSignalCount,
        int maximumUnacknowledgedAlarmCount)
    {
        if (!double.IsFinite(maximumAbsoluteMassClosureResidualKilograms) || maximumAbsoluteMassClosureResidualKilograms < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAbsoluteMassClosureResidualKilograms));
        }
        if (!double.IsFinite(maximumAbsoluteFullEnergyPathClosureResidualJoules) || maximumAbsoluteFullEnergyPathClosureResidualJoules < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAbsoluteFullEnergyPathClosureResidualJoules));
        }
        if (maximumInvalidMeasuredSignalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumInvalidMeasuredSignalCount));
        }
        if (maximumUnacknowledgedAlarmCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumUnacknowledgedAlarmCount));
        }

        MaximumAbsoluteMassClosureResidualKilograms = maximumAbsoluteMassClosureResidualKilograms;
        MaximumAbsoluteFullEnergyPathClosureResidualJoules = maximumAbsoluteFullEnergyPathClosureResidualJoules;
        MaximumInvalidMeasuredSignalCount = maximumInvalidMeasuredSignalCount;
        MaximumUnacknowledgedAlarmCount = maximumUnacknowledgedAlarmCount;
    }

    public double MaximumAbsoluteMassClosureResidualKilograms { get; }
    public double MaximumAbsoluteFullEnergyPathClosureResidualJoules { get; }
    public int MaximumInvalidMeasuredSignalCount { get; }
    public int MaximumUnacknowledgedAlarmCount { get; }
}
