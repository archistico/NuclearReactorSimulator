using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults.ElectricalLoss;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;
using NuclearReactorSimulator.Application.Scenarios.Faults.LossOfCoolant;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.Scenarios.SafetyResponse;

/// <summary>
/// M8.7 capstone safety-response pack. It composes already-owned M8 faults with M7.7 observational training/evaluation;
/// it adds no new fault physics, protection rules, controller ownership or scripted physical outcomes.
/// </summary>
public static class SafetyResponseScenarioPack
{
    private static readonly ControlRoomCommandKind[] SafetyActions =
    {
        ControlRoomCommandKind.ReactorScram,
        ControlRoomCommandKind.TurbineTrip,
        ControlRoomCommandKind.GeneratorTrip,
        ControlRoomCommandKind.GeneratorBreakerOpen,
        ControlRoomCommandKind.GeneratorLoadLower,
        ControlRoomCommandKind.ControlRodInsert,
        ControlRoomCommandKind.ControlRodHold,
        ControlRoomCommandKind.MainCirculationPumpStart,
        ControlRoomCommandKind.MainCirculationPumpStop,
        ControlRoomCommandKind.AlarmAcknowledge,
        ControlRoomCommandKind.AlarmAcknowledgeAll,
    };

    public static SafetyResponseExercise ProtectionFailSafe { get; } = CreateProtectionFailSafe();

    public static SafetyResponseExercise LargeBreakClass { get; } = CreateLargeBreakClass();

    public static SafetyResponseExercise StationBlackoutClass { get; } = CreateStationBlackoutClass();

    public static IReadOnlyList<SafetyResponseExercise> All { get; } = new[]
    {
        ProtectionFailSafe,
        LargeBreakClass,
        StationBlackoutClass,
    };

    private static SafetyResponseExercise CreateProtectionFailSafe()
    {
        const string scenarioId = "m87-protection-fail-safe-response";
        var source = InstrumentationControlFaultScenarioPack.ProtectionDiagnostic;
        var scenario = new ScenarioDefinition(
            scenarioId,
            "Protection Fail-Safe Response",
            "Capstone safety-response exercise using the validated M8.3 unavailable protection measurement to verify committed-frame instrumentation degradation, canonical protection response and operator alarm handling.",
            source.InitialCondition,
            new[]
            {
                Objective("recognize-initiator", "Recognize the initiating fault", "Identify the active instrumentation fault and resulting degraded measured-signal state."),
                Objective("verify-protection", "Verify canonical protection response", "Observe the M5.5 reactor SCRAM response without scenario-owned trip injection."),
                Objective("operator-response", "Record disciplined operator response", "Use the accepted operator-action timeline and acknowledge annunciated alarms without resetting protection implicitly."),
            },
            SafetyActions,
            source.Faults);

        var plan = new ScenarioTrainingPlan(
            scenarioId,
            new[]
            {
                Checkpoint("initiator-active", "Initiator active", "The declared protection-channel fault became active.", Fault("m83-pressure-unavailable-diagnostic")),
                Checkpoint("measurement-degraded", "Measurement degraded", "At least one committed measured signal is invalid.", SafetyResponseCheckpointEvaluator.InvalidMeasurementPresentCheckId, "initiator-active"),
                Checkpoint("scram-active", "SCRAM response", "The canonical reactor SCRAM latch became active.", SafetyResponseCheckpointEvaluator.ReactorScramActiveCheckId, "measurement-degraded"),
                Checkpoint("alarms-annunciated", "Alarms annunciated", "Alarm presentation reflects the abnormal/protection response.", SafetyResponseCheckpointEvaluator.AlarmAnnunciatedCheckId, "scram-active"),
            },
            new[]
            {
                Criterion("c-initiator", "Initiator recognized", "initiator-active"),
                Criterion("c-measurement", "Measurement degradation observed", "measurement-degraded"),
                Criterion("c-scram", "Canonical SCRAM observed", "scram-active"),
                Criterion("c-alarms", "Alarm response observed", "alarms-annunciated"),
                Action("c-ack", "Alarm acknowledgement recorded", ControlRoomCommandKind.AlarmAcknowledgeAll),
            },
            new[]
            {
                ObjectiveEvaluation("recognize-initiator", 25, "c-initiator", "c-measurement"),
                ObjectiveEvaluation("verify-protection", 45, "c-scram", "c-alarms"),
                ObjectiveEvaluation("operator-response", 30, "c-ack"),
            });
        return new SafetyResponseExercise(scenario, plan);
    }

    private static SafetyResponseExercise CreateLargeBreakClass()
    {
        const string scenarioId = "m87-large-break-safety-response";
        var source = LossOfCoolantScenarioPack.LargeBreakClass;
        var scenario = new ScenarioDefinition(
            scenarioId,
            "Large Break-Class Safety Response",
            "Capstone response exercise over the M8.5 bounded educational large-break-class source-term model. Acceptance criteria observe fault/protection/isolation state without claiming licensing-grade LOCA fidelity.",
            source.InitialCondition,
            new[]
            {
                Objective("recognize-initiator", "Recognize inventory-loss initiator", "Identify the active pressure-driven break and abnormal plant response."),
                Objective("verify-protection", "Verify protection/isolation response", "Observe canonical trip state and generator isolation as consequences of protection and/or accepted operator action."),
                Objective("operator-response", "Record conservative operator response", "Capture accepted safety actions and alarm acknowledgement in deterministic logical order."),
            },
            SafetyActions,
            source.Faults);

        var plan = new ScenarioTrainingPlan(
            scenarioId,
            new[]
            {
                Checkpoint("initiator-active", "Break active", "The declared pressure-driven break became active.", Fault("m85-large-break-event")),
                Checkpoint("protection-active", "Protection active", "At least one canonical protection trip has been observed.", SafetyResponseCheckpointEvaluator.AnyTripActiveCheckId, "initiator-active"),
                Checkpoint("generator-isolated", "Generator isolated", "All published generator breakers are open.", SafetyResponseCheckpointEvaluator.GeneratorBreakerOpenCheckId, "protection-active"),
            },
            new[]
            {
                Criterion("c-initiator", "Break initiator observed", "initiator-active"),
                Criterion("c-protection", "Protection response observed", "protection-active"),
                Criterion("c-isolation", "Generator isolation observed", "generator-isolated"),
                Action("c-ack", "Alarm acknowledgement recorded", ControlRoomCommandKind.AlarmAcknowledgeAll),
            },
            new[]
            {
                ObjectiveEvaluation("recognize-initiator", 25, "c-initiator"),
                ObjectiveEvaluation("verify-protection", 45, "c-protection", "c-isolation"),
                ObjectiveEvaluation("operator-response", 30, "c-ack"),
            });
        return new SafetyResponseExercise(scenario, plan);
    }

    private static SafetyResponseExercise CreateStationBlackoutClass()
    {
        const string scenarioId = "m87-station-blackout-safety-response";
        var source = ElectricalLossScenarioPack.StationBlackoutClass;
        var scenario = new ScenarioDefinition(
            scenarioId,
            "Station Blackout-Class Safety Response",
            "Capstone response exercise over the M8.6 composed external-supply/pump/control/turbine/generator fault set, with explicit limits for unmodeled station buses, emergency power and decay-heat runtime integration.",
            source.InitialCondition,
            new[]
            {
                Objective("recognize-initiator", "Recognize electrical-loss initiator", "Identify external-supply loss and the explicitly declared powered-equipment consequences."),
                Objective("verify-protection", "Verify isolation/protection response", "Observe canonical breaker isolation and trip response without inferring unmodeled electrical distribution behavior."),
                Objective("operator-response", "Record disciplined blackout-class response", "Capture accepted protective/operator actions and alarm acknowledgement in deterministic logical order."),
            },
            SafetyActions,
            source.Faults);

        var plan = new ScenarioTrainingPlan(
            scenarioId,
            new[]
            {
                Checkpoint("grid-loss-active", "External supply lost", "The declared external-supply fault became active.", Fault("m86-sbo-grid-loss")),
                Checkpoint("circulation-loss-active", "Main circulation trip active", "The declared main-circulation pump trip is active.", Fault("m86-sbo-main-circulation-trip"), "grid-loss-active"),
                Checkpoint("protection-active", "Protection active", "At least one canonical protection trip has been observed.", SafetyResponseCheckpointEvaluator.AnyTripActiveCheckId, "grid-loss-active"),
                Checkpoint("generator-isolated", "Generator isolated", "All published generator breakers are open.", SafetyResponseCheckpointEvaluator.GeneratorBreakerOpenCheckId, "protection-active"),
            },
            new[]
            {
                Criterion("c-grid-loss", "External-supply loss observed", "grid-loss-active"),
                Criterion("c-circulation-loss", "Circulation consequence observed", "circulation-loss-active"),
                Criterion("c-protection", "Protection response observed", "protection-active"),
                Criterion("c-isolation", "Generator isolation observed", "generator-isolated"),
                Action("c-ack", "Alarm acknowledgement recorded", ControlRoomCommandKind.AlarmAcknowledgeAll),
            },
            new[]
            {
                ObjectiveEvaluation("recognize-initiator", 25, "c-grid-loss", "c-circulation-loss"),
                ObjectiveEvaluation("verify-protection", 45, "c-protection", "c-isolation"),
                ObjectiveEvaluation("operator-response", 30, "c-ack"),
            });
        return new SafetyResponseExercise(scenario, plan);
    }

    private static string Fault(string faultId) => SafetyResponseCheckpointEvaluator.FaultActivePrefix + faultId;

    private static ScenarioObjectiveDefinition Objective(string id, string title, string description)
        => new(id, title, description);

    private static TrainingCheckpointDefinition Checkpoint(
        string id,
        string title,
        string description,
        string sourceCheckId,
        params string[] requiredPriorCheckpointIds)
        => new(id, title, description, sourceCheckId, requiredPriorCheckpointIds);

    private static TrainingEvaluationCriterionDefinition Criterion(string id, string title, string checkpointId)
        => new(id, title, title, TrainingEvaluationCriterionKind.CheckpointSatisfied, checkpointId);

    private static TrainingEvaluationCriterionDefinition Action(string id, string title, ControlRoomCommandKind action)
        => new(id, title, title, TrainingEvaluationCriterionKind.OperatorActionObserved, operatorActions: new[] { action });

    private static TrainingObjectiveEvaluationDefinition ObjectiveEvaluation(string id, int points, params string[] criterionIds)
        => new(id, points, criterionIds);
}
