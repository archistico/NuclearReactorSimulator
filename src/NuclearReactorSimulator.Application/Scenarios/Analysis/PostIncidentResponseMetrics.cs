namespace NuclearReactorSimulator.Application.Scenarios.Analysis;

/// <summary>
/// Deterministic temporal metrics derived from observed recorder facts. Nullable latency means the response was not observed
/// inside the selected analysis window; it never implies that the response could not occur later.
/// </summary>
public sealed record PostIncidentResponseMetrics
{
    public PostIncidentResponseMetrics(
        long? firstAlarmLatencySteps,
        long? firstProtectionActivationLatencySteps,
        long? firstOperatorActionLatencySteps,
        long? firstFaultClearLatencySteps,
        int peakInvalidMeasuredSignalCount,
        int peakAnnunciatedAlarmCount,
        int peakUnacknowledgedAlarmCount,
        int peakActiveFaultCount)
    {
        ValidateLatency(firstAlarmLatencySteps, nameof(firstAlarmLatencySteps));
        ValidateLatency(firstProtectionActivationLatencySteps, nameof(firstProtectionActivationLatencySteps));
        ValidateLatency(firstOperatorActionLatencySteps, nameof(firstOperatorActionLatencySteps));
        ValidateLatency(firstFaultClearLatencySteps, nameof(firstFaultClearLatencySteps));
        if (peakInvalidMeasuredSignalCount < 0
            || peakAnnunciatedAlarmCount < 0
            || peakUnacknowledgedAlarmCount < 0
            || peakActiveFaultCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(peakInvalidMeasuredSignalCount));
        }
        if (peakUnacknowledgedAlarmCount > peakAnnunciatedAlarmCount)
        {
            throw new ArgumentException("Peak unacknowledged alarms cannot exceed peak annunciated alarms.");
        }

        FirstAlarmLatencySteps = firstAlarmLatencySteps;
        FirstProtectionActivationLatencySteps = firstProtectionActivationLatencySteps;
        FirstOperatorActionLatencySteps = firstOperatorActionLatencySteps;
        FirstFaultClearLatencySteps = firstFaultClearLatencySteps;
        PeakInvalidMeasuredSignalCount = peakInvalidMeasuredSignalCount;
        PeakAnnunciatedAlarmCount = peakAnnunciatedAlarmCount;
        PeakUnacknowledgedAlarmCount = peakUnacknowledgedAlarmCount;
        PeakActiveFaultCount = peakActiveFaultCount;
    }

    public long? FirstAlarmLatencySteps { get; }
    public long? FirstProtectionActivationLatencySteps { get; }
    public long? FirstOperatorActionLatencySteps { get; }
    public long? FirstFaultClearLatencySteps { get; }
    public int PeakInvalidMeasuredSignalCount { get; }
    public int PeakAnnunciatedAlarmCount { get; }
    public int PeakUnacknowledgedAlarmCount { get; }
    public int PeakActiveFaultCount { get; }

    private static void ValidateLatency(long? value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
