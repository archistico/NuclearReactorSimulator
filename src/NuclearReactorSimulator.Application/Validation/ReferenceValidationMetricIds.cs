namespace NuclearReactorSimulator.Application.Validation;

/// <summary>Stable presentation-level metric IDs used by M9.6 reference baselines.</summary>
public static class ReferenceValidationMetricIds
{
    public const string ReactorThermalPowerMw = "reactor.thermal-power-mw";
    public const string ReactorAverageRodWithdrawalPercent = "reactor.average-rod-withdrawal-percent";
    public const string ReactorXenonReactivityPcm = "reactor.xenon-reactivity-pcm";
    public const string PrimaryTotalMassKg = "primary.total-mass-kg";
    public const string PrimaryRunningPumpCount = "primary.running-pump-count";
    public const string SecondaryMaximumRotorSpeedRpm = "secondary.maximum-rotor-speed-rpm";
    public const string SecondaryTurbineShaftPowerMw = "secondary.turbine-shaft-power-mw";
    public const string ElectricalGrossOutputMwe = "electrical.gross-output-mwe";
    public const string ElectricalTotalGeneratorOutputMwe = "electrical.total-generator-output-mwe";
    public const string ElectricalClosedBreakerCount = "electrical.closed-breaker-count";
    public const string ElectricalSynchronizationReadyGeneratorCount = "electrical.synchronization-ready-generator-count";
    public const string InstrumentationInvalidSignalCount = "instrumentation.invalid-signal-count";
    public const string AlarmUnacknowledgedCount = "alarms.unacknowledged-count";
    public const string ProtectionReactorScramActive = "protection.reactor-scram-active";
    public const string ProtectionTurbineTripActive = "protection.turbine-trip-active";
    public const string ProtectionGeneratorTripActive = "protection.generator-trip-active";
}
