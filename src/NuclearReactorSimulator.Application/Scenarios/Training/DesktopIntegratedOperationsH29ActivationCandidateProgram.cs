namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// Historical H.29 candidate scenario identity. Its metadata remains unchanged after H.30 Requalification 1 so archives,
/// replay evidence and the original activation-candidate provenance are not reinterpreted. Production activation uses a
/// separate H.30 RQ1 scenario identity over the same exact-v3 initial condition.
/// </summary>
public static class DesktopIntegratedOperationsH29ActivationCandidateProgram
{
    private const string ScenarioId = "integrated-normal-operations-training-h29-activation-candidate";

    public static ScenarioDefinition Scenario { get; } = new(
        ScenarioId,
        "Integrated Normal Operations Training — H.29 Corrected-Commit Candidate",
        "H.29 production-activation candidate using the validated desktop objectives/actions over exact initial-condition version 3. Version 2 remains the explicit production reference and rollback target until the H.30 closure decision.",
        DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference,
        DesktopIntegratedOperationsProgram.Scenario.Objectives,
        DesktopIntegratedOperationsProgram.Scenario.AllowedOperatorActions);

    public static ScenarioTrainingPlan TrainingPlan { get; } = new(
        ScenarioId,
        DesktopIntegratedOperationsProgram.TrainingPlan.Checkpoints,
        DesktopIntegratedOperationsProgram.TrainingPlan.Criteria,
        DesktopIntegratedOperationsProgram.TrainingPlan.Objectives,
        DesktopIntegratedOperationsProgram.TrainingPlan.Penalties);
}
