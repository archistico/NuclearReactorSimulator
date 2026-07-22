namespace NuclearReactorSimulator.Application.Scenarios.Historical;

/// <summary>
/// Stable M9.5 identifiers for validated simulator capabilities that historical-inspired content may explicitly require.
/// These are fidelity declarations, not runtime feature toggles and not substitutes for the canonical subsystem owners.
/// </summary>
public static class HistoricalModelCapabilityIds
{
    public const string DeterministicFullPlantRuntime = "runtime.deterministic-full-plant";
    public const string GlobalPointKinetics = "reactor.global-point-kinetics";
    public const string IntegratedPrimaryCircuit = "primary.integrated-thermohydraulics";
    public const string IntegratedSecondaryCycle = "secondary.integrated-steam-turbine-feedwater";
    public const string GeneratorGridSynchronization = "electrical.generator-grid-synchronization";
    public const string MeasuredInstrumentation = "instrumentation.measured-signals";
    public const string AutomaticControlProtectionAlarms = "control.automatic-protection-alarms";
    public const string VersionedInitialConditions = "scenario.versioned-initial-conditions";
    public const string TrainingGuidanceEvaluation = "training.guidance-evaluation";
    public const string DeterministicFaultInjection = "scenario.deterministic-fault-injection";
    public const string EducationalLeakLoca = "scenario.educational-leak-loca";
    public const string ElectricalLossStationBlackout = "scenario.electrical-loss-station-blackout";
    public const string RecorderCheckpointReplay = "analysis.recorder-checkpoint-replay";
    public const string PostIncidentAnalysis = "analysis.post-incident";
    public const string IodineXenon = "reactor.iodine-xenon";
    public const string QuasiSpatialCoreFeedback = "reactor.quasi-spatial-core-feedback";

    public static IReadOnlySet<string> ValidatedThroughM94 { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        DeterministicFullPlantRuntime,
        GlobalPointKinetics,
        IntegratedPrimaryCircuit,
        IntegratedSecondaryCycle,
        GeneratorGridSynchronization,
        MeasuredInstrumentation,
        AutomaticControlProtectionAlarms,
        VersionedInitialConditions,
        TrainingGuidanceEvaluation,
        DeterministicFaultInjection,
        EducationalLeakLoca,
        ElectricalLossStationBlackout,
        RecorderCheckpointReplay,
        PostIncidentAnalysis,
        IodineXenon,
        QuasiSpatialCoreFeedback,
    };
}
