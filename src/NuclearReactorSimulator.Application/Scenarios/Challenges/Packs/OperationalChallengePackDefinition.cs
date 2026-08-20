using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

using NuclearReactorSimulator.Application.Scenarios;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;

/// <summary>
/// Immutable M10.9.6.4 composition of one already-supported scenario, one challenge contract, one observational
/// condition evaluator and one exact scoring policy. The pack owns no simulation, command-dispatch or protection seam.
/// </summary>
public sealed class OperationalChallengePackDefinition
{
    private readonly IReadOnlyList<OperationalChallengeScoreEvidenceBinding> _scoreEvidenceBindings;

    public OperationalChallengePackDefinition(
        string packId,
        int version,
        ScenarioDefinition scenario,
        ChallengeDefinition challenge,
        IChallengeConditionEvaluator conditionEvaluator,
        ChallengeScoringPolicyDefinition scoringPolicy,
        IEnumerable<OperationalChallengeScoreEvidenceBinding> scoreEvidenceBindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
        ConditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
        ScoringPolicy = scoringPolicy ?? throw new ArgumentNullException(nameof(scoringPolicy));
        ArgumentNullException.ThrowIfNull(scoreEvidenceBindings);

        if (!string.Equals(challenge.ScenarioId, scenario.ScenarioId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Challenge scenario identity must match the composed scenario.", nameof(challenge));
        }
        if (!scenario.Objectives.Any(objective => string.Equals(objective.ObjectiveId, challenge.ObjectiveId, StringComparison.Ordinal)))
        {
            throw new ArgumentException($"Challenge objective '{challenge.ObjectiveId}' is not declared by scenario '{scenario.ScenarioId}'.", nameof(challenge));
        }
        if (!string.Equals(challenge.AssistancePolicy.ScoringPolicyId, scoringPolicy.ExactId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Challenge scoring-policy identity must match the composed exact scoring policy.", nameof(scoringPolicy));
        }

        var bindings = scoreEvidenceBindings.ToArray();
        if (bindings.Length == 0 || bindings.Any(static item => item is null))
        {
            throw new ArgumentException("A challenge pack requires non-null score-evidence bindings.", nameof(scoreEvidenceBindings));
        }
        if (bindings.Select(static item => item.Kind).Distinct().Count() != bindings.Length)
        {
            throw new ArgumentException("Challenge-pack score-evidence dimensions must be unique.", nameof(scoreEvidenceBindings));
        }
        if (bindings.Select(static item => item.EvidenceSourceId).Distinct(StringComparer.Ordinal).Count() != bindings.Length)
        {
            throw new ArgumentException("Challenge-pack score-evidence source IDs must be unique.", nameof(scoreEvidenceBindings));
        }
        var policyKinds = scoringPolicy.Dimensions.Select(static item => item.Kind).OrderBy(static item => item).ToArray();
        var bindingKinds = bindings.Select(static item => item.Kind).OrderBy(static item => item).ToArray();
        if (!policyKinds.SequenceEqual(bindingKinds))
        {
            throw new ArgumentException("Challenge-pack score-evidence bindings must cover every scoring-policy dimension exactly once.", nameof(scoreEvidenceBindings));
        }

        var policyHasDemand = scoringPolicy.Dimensions.Any(static item => item.Kind == ChallengeScoreDimensionKind.DemandTracking);
        if (challenge.ExternalDemandProfile is null && policyHasDemand)
        {
            throw new ArgumentException("A challenge without an external-demand profile cannot use a scoring policy that requires demand tracking.", nameof(scoringPolicy));
        }
        if (challenge.ExternalDemandProfile is not null && !policyHasDemand)
        {
            throw new ArgumentException("A challenge with an external-demand profile must use a scoring policy that declares demand tracking.", nameof(scoringPolicy));
        }

        PackId = packId.Trim();
        Version = version;
        _scoreEvidenceBindings = Array.AsReadOnly(bindings);
    }

    public string PackId { get; }
    public int Version { get; }
    public string ExactId => $"{PackId}@{Version}";
    public ScenarioDefinition Scenario { get; }
    public ChallengeDefinition Challenge { get; }
    public IChallengeConditionEvaluator ConditionEvaluator { get; }
    public ChallengeScoringPolicyDefinition ScoringPolicy { get; }
    public IReadOnlyList<OperationalChallengeScoreEvidenceBinding> ScoreEvidenceBindings => _scoreEvidenceBindings;
}
