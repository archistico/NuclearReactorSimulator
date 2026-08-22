using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;

/// <summary>
/// Production-safe exact challenge bindings introduced after integrated validation exposed that the historical @1
/// power-manoeuvring seed still uses the pre-I.5 thermodynamic/hydraulic runtime. Historical packs remain immutable
/// and replayable; these exact versions change only the composed scenario/initial-condition identity.
/// </summary>
public static class ProductionOperationalChallengePack
{
    public static OperationalChallengePackDefinition BoundedDemandFollowing { get; } = RebindToProductionScenario(
        InitialOperationalChallengePack.BoundedDemandFollowing,
        DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario);

    public static IReadOnlyList<OperationalChallengePackDefinition> All { get; } = new[]
    {
        BoundedDemandFollowing,
    };

    private static OperationalChallengePackDefinition RebindToProductionScenario(
        OperationalChallengePackDefinition historical,
        ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(historical);
        ArgumentNullException.ThrowIfNull(scenario);

        var source = historical.Challenge;
        var challenge = new ChallengeDefinition(
            source.ChallengeId,
            source.Version + 1,
            scenario.ScenarioId,
            source.ObjectiveId,
            source.Title,
            source.Description,
            source.ActivationCondition,
            source.RequiredObservations,
            source.CompletionConditions,
            source.FailureConditions,
            source.LogicalTime,
            source.AssistancePolicy,
            source.ExternalDemandProfile);

        return new OperationalChallengePackDefinition(
            historical.PackId,
            historical.Version + 1,
            scenario,
            challenge,
            historical.ConditionEvaluator,
            historical.ScoringPolicy,
            historical.ScoreEvidenceBindings);
    }
}
