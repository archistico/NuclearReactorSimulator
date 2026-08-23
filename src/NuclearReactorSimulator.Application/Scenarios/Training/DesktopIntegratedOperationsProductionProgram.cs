using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// Current production-facing desktop program selector. Historical scenario identities remain immutable: H.30 exact-v3,
/// I.5 exact-v4 and the M10 Final exact-v9 activation-candidate scenario remain replayable. After the separate activation
/// decision, a distinct exact-v9 production scenario is authoritative. Exact-v2 remains fail-closed rollback/reference.
/// </summary>
public static class DesktopIntegratedOperationsProductionProgram
{
    private const string H30ProductionScenarioId = "integrated-normal-operations-training-h30-rq1-production";
    private const string I5RepairedProductionScenarioId = "integrated-normal-operations-training-i5-repaired-v4-production";
    private const string M10FinalExactV9ProductionScenarioId = "integrated-normal-operations-training-m10-final-v9-production";

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

    public static ScenarioDefinition M10FinalExactV9ProductionScenario { get; } = new(
        M10FinalExactV9ProductionScenarioId,
        "Integrated Normal Operations Training — M10 Final Exact-v9 Production",
        "M10 Final authoritative desktop production scenario using qualified exact initial-condition version 9 with grid-paralleled droop integral-reference separation, explicit turbine moisture-drain ownership and the post-moisture analytical whole-cycle equilibrium. Exact versions 2, 3 and 4 plus the activation-candidate scenario remain immutable and replayable.",
        DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference,
        DesktopIntegratedOperationsProgram.Scenario.Objectives,
        DesktopIntegratedOperationsProgram.Scenario.AllowedOperatorActions);

    public static ScenarioTrainingPlan M10FinalExactV9ProductionTrainingPlan { get; } = new(
        M10FinalExactV9ProductionScenarioId,
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
                => M10FinalExactV9ProductionScenario,
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
            || string.Equals(scenarioId, DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId, StringComparison.Ordinal)
            || string.Equals(scenarioId, M10FinalExactV9ProductionScenario.ScenarioId, StringComparison.Ordinal);
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
        if (string.Equals(scenarioId, M10FinalExactV9ProductionScenario.ScenarioId, StringComparison.Ordinal))
        {
            return M10FinalExactV9ProductionTrainingPlan;
        }

        throw new KeyNotFoundException($"Scenario '{scenarioId}' is not a registered desktop production-training identity.");
    }

    public static PowerManoeuvringGuidancePlan ProcedureGuidance
        => DesktopIntegratedOperationsProgram.ProcedureGuidance;

    public static ITrainingCheckpointEvaluator CreateCheckpointEvaluator()
        => DesktopIntegratedOperationsProgram.CreateCheckpointEvaluator();
}
