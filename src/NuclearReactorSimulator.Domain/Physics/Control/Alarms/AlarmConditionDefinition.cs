using NuclearReactorSimulator.Domain.Physics.Control.Protection;

namespace NuclearReactorSimulator.Domain.Physics.Control.Alarms;

public abstract record AlarmConditionDefinition;

public sealed record MeasuredAlarmConditionDefinition : AlarmConditionDefinition
{
    public MeasuredAlarmConditionDefinition(
        string measurementChannelId,
        AlarmComparison comparison,
        double threshold,
        bool activeOnInvalidMeasurement = false)
    {
        if (string.IsNullOrWhiteSpace(measurementChannelId))
        {
            throw new ArgumentException("Measurement-channel id cannot be empty or whitespace.", nameof(measurementChannelId));
        }
        if (!Enum.IsDefined(typeof(AlarmComparison), comparison))
        {
            throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown alarm comparison.");
        }
        if (!double.IsFinite(threshold))
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Alarm threshold must be finite.");
        }

        MeasurementChannelId = measurementChannelId.Trim();
        Comparison = comparison;
        Threshold = threshold;
        ActiveOnInvalidMeasurement = activeOnInvalidMeasurement;
    }

    public string MeasurementChannelId { get; }
    public AlarmComparison Comparison { get; }
    public double Threshold { get; }
    public bool ActiveOnInvalidMeasurement { get; }
}

public sealed record ProtectionFunctionAlarmConditionDefinition : AlarmConditionDefinition
{
    public ProtectionFunctionAlarmConditionDefinition(string protectionFunctionId)
    {
        if (string.IsNullOrWhiteSpace(protectionFunctionId))
        {
            throw new ArgumentException("Protection-function id cannot be empty or whitespace.", nameof(protectionFunctionId));
        }
        ProtectionFunctionId = protectionFunctionId.Trim();
    }

    public string ProtectionFunctionId { get; }
}

public sealed record ProtectionActionAlarmConditionDefinition : AlarmConditionDefinition
{
    public ProtectionActionAlarmConditionDefinition(ProtectionAction action)
    {
        if (action == ProtectionAction.None
            || (action & ~(ProtectionAction.ReactorScram | ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip)) != ProtectionAction.None)
        {
            throw new ArgumentOutOfRangeException(nameof(action), action, "Alarm protection action must contain supported M5.5 actions.");
        }
        Action = action;
    }

    public ProtectionAction Action { get; }
}

public sealed record ProtectionInterlockAlarmConditionDefinition : AlarmConditionDefinition
{
    public ProtectionInterlockAlarmConditionDefinition(ProtectionInterlockAction action)
    {
        if (action == ProtectionInterlockAction.None
            || (action & ~(ProtectionInterlockAction.BlockRodWithdrawal
                | ProtectionInterlockAction.BlockTurbineAdmissionOpening
                | ProtectionInterlockAction.BlockGeneratorBreakerClose)) != ProtectionInterlockAction.None)
        {
            throw new ArgumentOutOfRangeException(nameof(action), action, "Alarm interlock action must contain supported M5.5 interlocks.");
        }
        Action = action;
    }

    public ProtectionInterlockAction Action { get; }
}
