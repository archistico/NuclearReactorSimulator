using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;

/// <summary>
/// Authored provenance binding for one score dimension in an initial operational challenge pack. It declares where
/// M10.9.6.5 score evidence must come from; it performs no score arithmetic and owns no plant authority.
/// </summary>
public sealed record OperationalChallengeScoreEvidenceBinding
{
    public OperationalChallengeScoreEvidenceBinding(
        ChallengeScoreDimensionKind kind,
        string evidenceSourceId,
        string description)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceSourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Kind = kind;
        EvidenceSourceId = evidenceSourceId.Trim();
        Description = description.Trim();
    }

    public ChallengeScoreDimensionKind Kind { get; }
    public string EvidenceSourceId { get; }
    public string Description { get; }
}
