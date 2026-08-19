using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// Current production-facing desktop program selector. Historical scenario identities remain immutable; H.30 RQ1 adds a
/// new production scenario identity over exact-v3 while exact-v2 and the historical H.29 candidate remain replayable.
/// </summary>
public static class DesktopIntegratedOperationsProductionProgram
{
    private const string ProductionScenarioId = "integrated-normal-operations-training-h30-rq1-production";

    public static ScenarioDefinition CorrectedProductionScenario { get; } = new(
        ProductionScenarioId,
        "Integrated Normal Operations Training — Corrected-Commit Production",
        "H.30 Requalification 1 production desktop scenario using exact initial-condition version 3 and the qualified four-node corrected-commit hydraulic path. Exact version 2 remains the explicit rollback/reference identity.",
        DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference,
        DesktopIntegratedOperationsProgram.Scenario.Objectives,
        DesktopIntegratedOperationsProgram.Scenario.AllowedOperatorActions);

    public static ScenarioTrainingPlan CorrectedProductionTrainingPlan { get; } = new(
        ProductionScenarioId,
        DesktopIntegratedOperationsProgram.TrainingPlan.Checkpoints,
        DesktopIntegratedOperationsProgram.TrainingPlan.Criteria,
        DesktopIntegratedOperationsProgram.TrainingPlan.Objectives,
        DesktopIntegratedOperationsProgram.TrainingPlan.Penalties);

    public static ScenarioDefinition Scenario
        => ResolveScenario(DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);

    public static ScenarioDefinition ResolveScenario(
        DesktopHydraulicProductionPolicy requestedPolicy,
        bool explicitKillRequested = false)
    {
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(requestedPolicy, explicitKillRequested);
        return decision.EffectivePolicy switch
        {
            DesktopHydraulicProductionPolicy.ExplicitCommittedState
                => DesktopIntegratedOperationsProgram.Scenario,
            DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate
                => CorrectedProductionScenario,
            _ => throw new ArgumentOutOfRangeException(nameof(requestedPolicy)),
        };
    }

    public static bool IsDesktopTrainingScenario(string scenarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        return string.Equals(scenarioId, DesktopIntegratedOperationsProgram.Scenario.ScenarioId, StringComparison.Ordinal)
            || string.Equals(scenarioId, DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId, StringComparison.Ordinal)
            || string.Equals(scenarioId, CorrectedProductionScenario.ScenarioId, StringComparison.Ordinal);
    }

    public static ScenarioTrainingPlan ResolveTrainingPlan(string scenarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        if (string.Equals(scenarioId, DesktopIntegratedOperationsProgram.Scenario.ScenarioId, StringComparison.Ordinal))
        {
            return DesktopIntegratedOperationsProgram.TrainingPlan;
        }
        if (string.Equals(scenarioId, DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId, StringComparison.Ordinal))
        {
            return DesktopIntegratedOperationsH29ActivationCandidateProgram.TrainingPlan;
        }
        if (string.Equals(scenarioId, CorrectedProductionScenario.ScenarioId, StringComparison.Ordinal))
        {
            return CorrectedProductionTrainingPlan;
        }

        throw new KeyNotFoundException($"Scenario '{scenarioId}' is not a registered desktop production-training identity.");
    }

    public static PowerManoeuvringGuidancePlan ProcedureGuidance
        => DesktopIntegratedOperationsProgram.ProcedureGuidance;

    public static ITrainingCheckpointEvaluator CreateCheckpointEvaluator()
        => DesktopIntegratedOperationsProgram.CreateCheckpointEvaluator();
}
