namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// H.29 candidate scenario identity. The existing desktop program remains pinned to the v2 explicit seed; this separate
/// scenario permits exact-version save/replay qualification of the v3 corrected-commit production candidate without
/// reinterpreting historical recordings or changing the current production default.
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
}
