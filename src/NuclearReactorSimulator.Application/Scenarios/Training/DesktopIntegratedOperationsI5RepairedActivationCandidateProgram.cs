namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// I.5 REV1 repaired exact-v4 activation-readiness scenario. It is a distinct replayable identity and does not
/// reinterpret the historical M9.7/H.29/H.30 desktop scenarios. Production remains on the H.30 exact-v3 scenario
/// until the final activation gate explicitly changes the selector.
/// </summary>
public static class DesktopIntegratedOperationsI5RepairedActivationCandidateProgram
{
    private const string ScenarioId = "integrated-normal-operations-training-i5-repaired-v4-activation-candidate";

    public static ScenarioDefinition Scenario { get; } = new(
        ScenarioId,
        "Integrated Normal Operations Training — I.5 Repaired v4 Activation Candidate",
        "I.5 REV1 activation-readiness scenario using exact initial-condition version 4: validated CorrelationConsistentInverseDomain thermodynamics with qualified four-node corrected-commit ownership. Historical exact v2/v3 identities remain unchanged and production selection is not switched by this candidate.",
        DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference,
        DesktopIntegratedOperationsProgram.Scenario.Objectives,
        DesktopIntegratedOperationsProgram.Scenario.AllowedOperatorActions);

    public static ScenarioTrainingPlan TrainingPlan { get; } = new(
        ScenarioId,
        DesktopIntegratedOperationsProgram.TrainingPlan.Checkpoints,
        DesktopIntegratedOperationsProgram.TrainingPlan.Criteria,
        DesktopIntegratedOperationsProgram.TrainingPlan.Objectives,
        DesktopIntegratedOperationsProgram.TrainingPlan.Penalties);
}
