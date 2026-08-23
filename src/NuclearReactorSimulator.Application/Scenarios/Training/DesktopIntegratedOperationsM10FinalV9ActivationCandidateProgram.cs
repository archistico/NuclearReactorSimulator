namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M10 Final exact-v9 qualified production-activation candidate. Diagnostic 11 Hotfix 2 qualified the authored
/// post-moisture whole-cycle equilibrium; this scenario exposes that exact identity for activation wiring and replay
/// qualification without changing the authoritative production default from exact v4.
/// </summary>
public static class DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram
{
    private const string ScenarioId = "integrated-normal-operations-training-m10-final-v9-activation-candidate";

    public static ScenarioDefinition Scenario { get; } = new(
        ScenarioId,
        "Integrated Normal Operations Training — M10 Final v9 Activation Candidate",
        "M10 Final production-activation candidate using qualified exact initial-condition version 9 with grid-paralleled droop integral-reference separation, explicit turbine moisture-drain ownership and the post-moisture analytical whole-cycle equilibrium. Exact v4 remains authoritative until a separate activation decision gate passes.",
        DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference,
        DesktopIntegratedOperationsProgram.Scenario.Objectives,
        DesktopIntegratedOperationsProgram.Scenario.AllowedOperatorActions);

    public static ScenarioTrainingPlan TrainingPlan { get; } = new(
        ScenarioId,
        DesktopIntegratedOperationsProgram.TrainingPlan.Checkpoints,
        DesktopIntegratedOperationsProgram.TrainingPlan.Criteria,
        DesktopIntegratedOperationsProgram.TrainingPlan.Objectives,
        DesktopIntegratedOperationsProgram.TrainingPlan.Penalties);
}
