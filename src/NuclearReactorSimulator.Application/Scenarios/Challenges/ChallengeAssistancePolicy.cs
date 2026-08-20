using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>
/// Challenge-owned declaration of permitted presentation assistance and the scoring-policy identity to be resolved later
/// by M10.9.6.3. It contains no score arithmetic and no plant-control authority.
/// </summary>
public sealed record ChallengeAssistancePolicy
{
    private readonly IReadOnlySet<TrainingGuidanceMode> _allowedModes;

    public ChallengeAssistancePolicy(IEnumerable<TrainingGuidanceMode> allowedModes, string scoringPolicyId)
    {
        ArgumentNullException.ThrowIfNull(allowedModes);
        ArgumentException.ThrowIfNullOrWhiteSpace(scoringPolicyId);
        var set = new HashSet<TrainingGuidanceMode>(allowedModes);
        if (set.Count == 0 || set.Any(static mode => !Enum.IsDefined(mode)))
        {
            throw new ArgumentException("Challenge assistance policy requires at least one defined guidance mode.", nameof(allowedModes));
        }

        _allowedModes = set;
        ScoringPolicyId = scoringPolicyId.Trim();
    }

    public IReadOnlySet<TrainingGuidanceMode> AllowedModes => _allowedModes;
    public string ScoringPolicyId { get; }

    public bool Allows(TrainingGuidanceMode mode) => _allowedModes.Contains(mode);
}
