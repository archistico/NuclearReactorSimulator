using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// Current production-facing desktop program selector. Historical scenario identities remain immutable: H.30 exact-v3
/// and I.5 exact-v4 remain replayable, exact-v4 stays authoritative, and M10 Final exact-v9 is exposed only as a qualified
/// activation candidate until a later default-switch decision. Exact-v2 remains the fail-closed rollback/reference identity.
/// </summary>
public static class DesktopIntegratedOperationsProductionProgram
{
    private const string H30ProductionScenarioId = "integrated-normal-operations-training-h30-rq1-production";
    private const string I5RepairedProductionScenarioId = "integrated-normal-operations-training-i5-repaired-v4-production";

    public static ScenarioDefinition CorrectedProductionScenario { get; } = new(
        H30ProductionScenarioId,
        "Integrated Normal Operations Training — Corrected-Commit Production",
        "Historical H.30 Requalification 1 production desktop scenario using exact initial-condition version 3 and the qualified four-node corrected-commit hydraulic path. It remains replayable after I.5 activation.",
        DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference,
        DesktopIntegratedOperationsProgram.Scenario.Objectives,
        DesktopIntegratedOperationsProgram.Scenario.AllowedOperatorActions);

    public static ScenarioTrainingPlan CorrectedProductionTrainingPlan { get; } = new(
        H30ProductionScenarioId,
        DesktopIntegratedOperationsProgram.TrainingPlan.Checkpoints,
        DesktopIntegratedOperationsProgram.TrainingPlan.Criteria,
        DesktopIntegratedOperationsProgram.TrainingPlan.Objectives,
        DesktopIntegratedOperationsProgram.TrainingPlan.Penalties);

    public static ScenarioDefinition RepairedProductionScenario { get; } = new(
        I5RepairedProductionScenarioId,
        "Integrated Normal Operations Training — Repaired Production",
        "I.5 repaired production desktop scenario using exact initial-condition version 4: validated CorrelationConsistentInverseDomain thermodynamics with qualified four-node corrected-commit ownership. Exact versions 2 and 3 remain immutable rollback/historical replay identities.",
        DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference,
        DesktopIntegratedOperationsProgram.Scenario.Objectives,
        DesktopIntegratedOperationsProgram.Scenario.AllowedOperatorActions);

    public static ScenarioTrainingPlan RepairedProductionTrainingPlan { get; } = new(
        I5RepairedProductionScenarioId,
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
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit
                => RepairedProductionScenario,
            DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate
                => DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario,
            _ => throw new ArgumentOutOfRangeException(nameof(requestedPolicy)),
        };
    }

    public static bool IsDesktopTrainingScenario(string scenarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        return string.Equals(scenarioId, DesktopIntegratedOperationsProgram.Scenario.ScenarioId, StringComparison.Ordinal)
            || string.Equals(scenarioId, DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId, StringComparison.Ordinal)
            || string.Equals(scenarioId, CorrectedProductionScenario.ScenarioId, StringComparison.Ordinal)
            || string.Equals(scenarioId, DesktopIntegratedOperationsI5RepairedActivationCandidateProgram.Scenario.ScenarioId, StringComparison.Ordinal)
            || string.Equals(scenarioId, RepairedProductionScenario.ScenarioId, StringComparison.Ordinal)
            || string.Equals(scenarioId, DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId, StringComparison.Ordinal);
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
        if (string.Equals(scenarioId, DesktopIntegratedOperationsI5RepairedActivationCandidateProgram.Scenario.ScenarioId, StringComparison.Ordinal))
        {
            return DesktopIntegratedOperationsI5RepairedActivationCandidateProgram.TrainingPlan;
        }
        if (string.Equals(scenarioId, RepairedProductionScenario.ScenarioId, StringComparison.Ordinal))
        {
            return RepairedProductionTrainingPlan;
        }
        if (string.Equals(scenarioId, DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId, StringComparison.Ordinal))
        {
            return DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.TrainingPlan;
        }

        throw new KeyNotFoundException($"Scenario '{scenarioId}' is not a registered desktop production-training identity.");
    }

    public static PowerManoeuvringGuidancePlan ProcedureGuidance
        => DesktopIntegratedOperationsProgram.ProcedureGuidance;

    public static ITrainingCheckpointEvaluator CreateCheckpointEvaluator()
        => DesktopIntegratedOperationsProgram.CreateCheckpointEvaluator();
}
