using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M7.7 capstone normal-operations training program. It reuses the validated M7.6 physical starting point and command
/// permissions, then adds only guidance/evaluation metadata over snapshots and accepted operator actions.
/// </summary>
public static class IntegratedOperationsTrainingProgram
{
    public static ScenarioDefinition Scenario { get; } = new(
        "integrated-normal-operations-training",
        "Integrated Normal Operations Training",
        "Capstone M7 training session for deliberate low-load manoeuvring, feedback observation and controlled normal shutdown with deterministic objective scoring and optional procedure guidance.",
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("verify-low-load", "Verify stable low-load operation", "Confirm the validated parallel low-load handoff before deliberate operator action."),
            new ScenarioObjectiveDefinition("manoeuvre-power", "Manoeuvre power deliberately", "Raise and reduce electrical load in the correct operational order through validated command seams."),
            new ScenarioObjectiveDefinition("observe-feedback", "Observe plant feedback", "Demonstrate awareness of temperature/void response while preserving the explicit xenon availability boundary."),
            new ScenarioObjectiveDefinition("normal-shutdown", "Perform normal shutdown", "Unload, disconnect, shut down the reactor and preserve post-shutdown circulation without substituting emergency trips for routine procedure."),
        },
        PowerManoeuvringNormalShutdownProgram.Scenario.AllowedOperatorActions);

    public static PowerManoeuvringGuidancePlan ProcedureGuidance { get; } = PowerManoeuvringNormalShutdownProgram.Guidance;

    public static ScenarioTrainingPlan TrainingPlan { get; } = new(
        Scenario.ScenarioId,
        new[]
        {
            Checkpoint("stable-low-load", "Stable low-load handoff", "Validated low-load parallel condition has been observed.", "low-load"),
            Checkpoint("increased-load", "Deliberate load increase", "The increased-load operating band has been reached.", "load-increased", "stable-low-load"),
            Checkpoint("reduced-load", "Controlled load reduction", "Load has been reduced back toward the low-load/unload region.", "load-reduced", "increased-load"),
            Checkpoint("temperature-feedback", "Temperature feedback observed", "Published fuel/coolant temperatures remained observable.", "temperature-feedback", "increased-load"),
            Checkpoint("void-feedback", "Void feedback observed", "Published void diagnostics remained explicit and observable.", "void-feedback", "increased-load"),
            Checkpoint("xenon-boundary", "Xenon boundary respected", "Quantitative xenon remained explicitly unavailable rather than being reconstructed by Application.", "xenon-boundary", "increased-load"),
            Checkpoint("generator-unloaded", "Generator unloaded", "Electrical output reached the unload tolerance before disconnection.", "generator-unloaded", "reduced-load"),
            Checkpoint("generator-disconnected", "Generator disconnected", "Generator breaker open state has been observed.", "breakers-open", "generator-unloaded"),
            Checkpoint("reactor-shutdown", "Reactor shutdown", "Low shutdown power and essentially inserted rods have been observed.", "reactor-shutdown", "generator-disconnected"),
            Checkpoint("post-shutdown-cooling", "Post-shutdown circulation", "Shutdown with generator isolated and main circulation available has been observed.", "post-shutdown-cooling", "reactor-shutdown"),
        },
        new[]
        {
            Criterion("c-low-load", "Low-load handoff verified", TrainingEvaluationCriterionKind.CheckpointSatisfied, "stable-low-load"),
            Criterion("c-load-up", "Increased load reached", TrainingEvaluationCriterionKind.CheckpointSatisfied, "increased-load"),
            Criterion("c-load-down", "Reduced load reached", TrainingEvaluationCriterionKind.CheckpointSatisfied, "reduced-load"),
            ActionSequence("c-manoeuvre-order", "Load commands ordered", ControlRoomCommandKind.GeneratorLoadRaise, ControlRoomCommandKind.GeneratorLoadLower),
            Criterion("c-temperature", "Temperature feedback observed", TrainingEvaluationCriterionKind.CheckpointSatisfied, "temperature-feedback"),
            Criterion("c-void", "Void feedback observed", TrainingEvaluationCriterionKind.CheckpointSatisfied, "void-feedback"),
            Criterion("c-xenon", "Xenon boundary respected", TrainingEvaluationCriterionKind.CheckpointSatisfied, "xenon-boundary"),
            Criterion("c-unloaded", "Generator unloaded", TrainingEvaluationCriterionKind.CheckpointSatisfied, "generator-unloaded"),
            Criterion("c-disconnected", "Generator disconnected", TrainingEvaluationCriterionKind.CheckpointSatisfied, "generator-disconnected"),
            Criterion("c-reactor-shutdown", "Reactor shutdown achieved", TrainingEvaluationCriterionKind.CheckpointSatisfied, "reactor-shutdown"),
            Criterion("c-post-cooling", "Post-shutdown cooling established", TrainingEvaluationCriterionKind.CheckpointSatisfied, "post-shutdown-cooling"),
            ActionSequence("c-shutdown-order", "Shutdown actions ordered", ControlRoomCommandKind.GeneratorLoadLower, ControlRoomCommandKind.GeneratorBreakerOpen, ControlRoomCommandKind.ControlRodInsert),
        },
        new[]
        {
            new TrainingObjectiveEvaluationDefinition("verify-low-load", 15, new[] { "c-low-load" }),
            new TrainingObjectiveEvaluationDefinition("manoeuvre-power", 30, new[] { "c-load-up", "c-load-down", "c-manoeuvre-order" }),
            new TrainingObjectiveEvaluationDefinition("observe-feedback", 20, new[] { "c-temperature", "c-void", "c-xenon" }),
            new TrainingObjectiveEvaluationDefinition("normal-shutdown", 35, new[] { "c-unloaded", "c-disconnected", "c-reactor-shutdown", "c-post-cooling", "c-shutdown-order" }),
        },
        new[]
        {
            new TrainingPenaltyDefinition("routine-scram", "SCRAM used during normal procedure", "Emergency SCRAM is available for safety but is not a substitute for the controlled normal-shutdown sequence.", ControlRoomCommandKind.ReactorScram, 15),
            new TrainingPenaltyDefinition("routine-turbine-trip", "Turbine trip used during normal procedure", "Routine turbine rundown should use the validated governing seam unless an actual protective need exists.", ControlRoomCommandKind.TurbineTrip, 10),
            new TrainingPenaltyDefinition("routine-generator-trip", "Generator trip used during normal procedure", "Routine grid disconnection should unload and open the breaker rather than substitute a generator trip.", ControlRoomCommandKind.GeneratorTrip, 10),
        });

    public static ITrainingCheckpointEvaluator CreateCheckpointEvaluator()
        => new PowerManoeuvringTrainingCheckpointEvaluator(ProcedureGuidance);

    private static TrainingCheckpointDefinition Checkpoint(
        string id,
        string title,
        string description,
        string sourceCheckId,
        params string[] requiredPriorCheckpointIds)
        => new(id, title, description, sourceCheckId, requiredPriorCheckpointIds);

    private static TrainingEvaluationCriterionDefinition Criterion(
        string id,
        string title,
        TrainingEvaluationCriterionKind kind,
        string checkpointId)
        => new(id, title, title, kind, checkpointId);

    private static TrainingEvaluationCriterionDefinition ActionSequence(
        string id,
        string title,
        params ControlRoomCommandKind[] actions)
        => new(id, title, title, TrainingEvaluationCriterionKind.OperatorActionSequenceObserved, operatorActions: actions);
}
