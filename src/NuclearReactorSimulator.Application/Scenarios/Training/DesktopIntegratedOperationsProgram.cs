using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M9.7 desktop integration wrapper around the validated M7.7 training content. The scenario/plan use a new identity so the
/// historical M7.7 scenario and its exact v1 initial-condition identity remain replay-stable and untouched.
/// </summary>
public static class DesktopIntegratedOperationsProgram
{
    private const string ScenarioId = "integrated-normal-operations-training-m97-desktop";

    public static ScenarioDefinition Scenario { get; } = new(
        ScenarioId,
        "Integrated Normal Operations Training — Desktop Stable Runtime",
        "M9.7 desktop integration profile reusing the validated M7.7 objectives/actions over a separately versioned stable low-load runtime seed suitable for continuous RUN-mode and GUI validation.",
        DesktopIntegratedOperationsInitialConditionFactory.Reference,
        IntegratedOperationsTrainingProgram.Scenario.Objectives,
        IntegratedOperationsTrainingProgram.Scenario.AllowedOperatorActions);

    public static ScenarioTrainingPlan TrainingPlan { get; } = new(
        ScenarioId,
        IntegratedOperationsTrainingProgram.TrainingPlan.Checkpoints,
        IntegratedOperationsTrainingProgram.TrainingPlan.Criteria,
        IntegratedOperationsTrainingProgram.TrainingPlan.Objectives,
        IntegratedOperationsTrainingProgram.TrainingPlan.Penalties);

    public static PowerManoeuvringGuidancePlan ProcedureGuidance { get; } = IntegratedOperationsTrainingProgram.ProcedureGuidance;

    public static ITrainingCheckpointEvaluator CreateCheckpointEvaluator()
        => IntegratedOperationsTrainingProgram.CreateCheckpointEvaluator();
}
