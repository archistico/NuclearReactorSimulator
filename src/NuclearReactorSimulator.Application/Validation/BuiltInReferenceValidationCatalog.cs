namespace NuclearReactorSimulator.Application.Validation;

/// <summary>
/// Curated M9.6 internal regression/reference cases anchored to already validated scenario behavior. These baselines are not
/// external historical measurements and must never be presented as licensing-grade or historically exact calibration evidence.
/// </summary>
public static class BuiltInReferenceValidationCatalog
{
    public const string ValidatedModelVersion = "NRS-M9.5-VALIDATED";

    public static ReferenceValidationCaseDefinition ColdShutdownSteadyState { get; } = new(
        "cold-shutdown-steady-state-v1",
        "Cold shutdown steady-state boundary",
        "Validates the canonical M7.2 cold-shutdown handoff at logical step zero.",
        ReferenceValidationCaseKind.SteadyState,
        ValidatedModelVersion,
        "Internal validated regression baseline from M7.2/M9.5; not an external historical measurement.",
        new[]
        {
            Target(ReferenceValidationMetricIds.ReactorAverageRodWithdrawalPercent, 0, 0d, 1e-8d),
            Target(ReferenceValidationMetricIds.PrimaryRunningPumpCount, 0, 0d, 0d),
            Target(ReferenceValidationMetricIds.SecondaryMaximumRotorSpeedRpm, 0, 0d, 1e-6d),
            Target(ReferenceValidationMetricIds.ElectricalClosedBreakerCount, 0, 0d, 0d),
            Target(ReferenceValidationMetricIds.InstrumentationInvalidSignalCount, 0, 0d, 0d),
            Target(ReferenceValidationMetricIds.ProtectionReactorScramActive, 0, 0d, 0d),
        });

    public static ReferenceValidationCaseDefinition GridSynchronizationSteadyState { get; } = new(
        "grid-synchronization-steady-state-v1",
        "Pre-synchronization steady-state boundary",
        "Validates the canonical M7.5 matched-speed synchronization handoff before breaker closure.",
        ReferenceValidationCaseKind.SteadyState,
        ValidatedModelVersion,
        "Internal validated regression baseline from M7.5/M9.5; not an external historical measurement.",
        new[]
        {
            Target(ReferenceValidationMetricIds.SecondaryMaximumRotorSpeedRpm, 0, 3_000d, 10d),
            Target(ReferenceValidationMetricIds.ElectricalSynchronizationReadyGeneratorCount, 0, 1d, 0d),
            Target(ReferenceValidationMetricIds.ElectricalClosedBreakerCount, 0, 0d, 0d),
            Target(ReferenceValidationMetricIds.ElectricalTotalGeneratorOutputMwe, 0, 0d, 0.001d),
            Target(ReferenceValidationMetricIds.InstrumentationInvalidSignalCount, 0, 0d, 0d),
        });

    public static ReferenceValidationCaseDefinition InitialGridLoadTransient { get; } = new(
        "initial-grid-load-transient-v1",
        "Initial generator loading transient",
        "Validates the deterministic first breaker-close/load-raise sequence already established by M7.5.",
        ReferenceValidationCaseKind.Transient,
        ValidatedModelVersion,
        "Internal validated regression baseline from M7.5/M9.5; not an external historical measurement.",
        new[]
        {
            Target(ReferenceValidationMetricIds.ElectricalClosedBreakerCount, 2, 1d, 0d),
            Target(ReferenceValidationMetricIds.ElectricalTotalGeneratorOutputMwe, 2, 5d, 0.2d),
            Target(ReferenceValidationMetricIds.ProtectionGeneratorTripActive, 2, 0d, 0d),
        });

    public static IReadOnlyList<ReferenceValidationCaseDefinition> All { get; } = new[]
    {
        ColdShutdownSteadyState,
        GridSynchronizationSteadyState,
        InitialGridLoadTransient,
    };

    public static ReferenceValidationSuiteDefinition ValidatedBaselineSuite { get; } = new(
        "nrs-m96-reference-suite-v1",
        ValidatedModelVersion,
        All);

    private static ReferenceValidationTarget Target(string metricId, long step, double value, double absoluteTolerance)
        => new(metricId, step, value, new ReferenceValidationToleranceBudget(absoluteTolerance));
}
